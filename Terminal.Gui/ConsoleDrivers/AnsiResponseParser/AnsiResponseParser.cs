#nullable enable

namespace Terminal.Gui;

internal abstract class AnsiResponseParserBase : IAnsiResponseParser
{
    protected object _lockExpectedResponses = new ();

    protected object _lockState = new ();

    /// <summary>
    ///     Responses we are expecting to come in.
    /// </summary>
    protected readonly List<AnsiResponseExpectation> _expectedResponses = [];

    /// <summary>
    ///     Collection of responses that we <see cref="StopExpecting"/>.
    /// </summary>
    protected readonly List<AnsiResponseExpectation> _lateResponses = [];

    /// <summary>
    ///     Responses that you want to look out for that will come in continuously e.g. mouse events.
    ///     Key is the terminator.
    /// </summary>
    protected readonly List<AnsiResponseExpectation> _persistentExpectations = [];

    private AnsiResponseParserState _state = AnsiResponseParserState.Normal;

    /// <inheritdoc/>
    public AnsiResponseParserState State
    {
        get => _state;
        protected set
        {
            StateChangedAt = DateTime.Now;
            _state = value;
        }
    }

    protected readonly IHeld _heldContent;

    /// <summary>
    ///     When <see cref="State"/> was last changed.
    /// </summary>
    public DateTime StateChangedAt { get; private set; } = DateTime.Now;

    // These all are valid terminators on ansi responses,
    // see CSI in https://invisible-island.net/xterm/ctlseqs/ctlseqs.html#h3-Functions-using-CSI-_-ordered-by-the-final-character_s
    // No - N or O
    protected readonly HashSet<char> _knownTerminators = new (
                                                              [
                                                                  '@', 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'G', 'H', 'I', 'J', 'K', 'L', 'M',

                                                                  // No - N or O
                                                                  'P', 'Q', 'R', 'S', 'T', 'W', 'X', 'Z',
                                                                  '^', '`', '~',
                                                                  'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i',
                                                                  'l', 'm', 'n',
                                                                  'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z'
                                                              ]);

    protected AnsiResponseParserBase (IHeld heldContent) { _heldContent = heldContent; }

    protected void ResetState ()
    {
        State = AnsiResponseParserState.Normal;

        lock (_lockState)
        {
            _heldContent.ClearHeld ();
        }
    }

    /// <summary>
    ///     Processes an input collection of objects <paramref name="inputLength"/> long.
    ///     You must provide the indexers to return the objects and the action to append
    ///     to output stream.
    /// </summary>
    /// <param name="getCharAtIndex">The character representation of element i of your input collection</param>
    /// <param name="getObjectAtIndex">The actual element in the collection (e.g. char or Tuple&lt;char,T&gt;)</param>
    /// <param name="appendOutput">
    ///     Action to invoke when parser confirms an element of the current collection or a previous
    ///     call's collection should be appended to the current output (i.e. append to your output List/StringBuilder).
    /// </param>
    /// <param name="inputLength">The total number of elements in your collection</param>
    protected void ProcessInputBase (
        Func<int, char> getCharAtIndex,
        Func<int, object> getObjectAtIndex,
        Action<object> appendOutput,
        int inputLength
    )
    {
        lock (_lockState)
        {
            ProcessInputBaseImpl (getCharAtIndex, getObjectAtIndex, appendOutput, inputLength);
        }
    }

    private void ProcessInputBaseImpl (
        Func<int, char> getCharAtIndex,
        Func<int, object> getObjectAtIndex,
        Action<object> appendOutput,
        int inputLength
    )
    {
        var index = 0; // Tracks position in the input string

        while (index < inputLength)
        {
            char currentChar = getCharAtIndex (index);
            object currentObj = getObjectAtIndex (index);

            bool isEscape = currentChar == '\x1B';

            switch (State)
            {
                case AnsiResponseParserState.Normal:
                    if (isEscape)
                    {
                        // Escape character detected, move to ExpectingBracket state
                        State = AnsiResponseParserState.ExpectingBracket;
                        _heldContent.AddToHeld (currentObj); // Hold the escape character
                    }
                    else
                    {
                        // Normal character, append to output
                        appendOutput (currentObj);
                    }

                    break;

                case AnsiResponseParserState.ExpectingBracket:
                    if (isEscape)
                    {
                        // Second escape so we must release first
                        ReleaseHeld (appendOutput, AnsiResponseParserState.ExpectingBracket);
                        _heldContent.AddToHeld (currentObj); // Hold the new escape
                    }
                    else if (currentChar == '[')
                    {
                        // Detected '[', transition to InResponse state
                        State = AnsiResponseParserState.InResponse;
                        _heldContent.AddToHeld (currentObj); // Hold the '['
                    }
                    else
                    {
                        // Invalid sequence, release held characters and reset to Normal
                        ReleaseHeld (appendOutput);
                        appendOutput (currentObj); // Add current character
                    }

                    break;

                case AnsiResponseParserState.InResponse:
                    _heldContent.AddToHeld (currentObj);

                    // Check if the held content should be released
                    if (ShouldReleaseHeldContent ())
                    {
                        ReleaseHeld (appendOutput);
                    }

                    break;
            }

            index++;
        }
    }

    private void ReleaseHeld (Action<object> appendOutput, AnsiResponseParserState newState = AnsiResponseParserState.Normal)
    {
        foreach (object o in _heldContent.HeldToObjects ())
        {
            appendOutput (o);
        }

        State = newState;
        _heldContent.ClearHeld ();
    }

    // Common response handler logic
    protected bool ShouldReleaseHeldContent ()
    {
        lock (_lockState)
        {
            string cur = _heldContent.HeldToString ();

            lock (_lockExpectedResponses)
            {
                // Look for an expected response for what is accumulated so far (since Esc)
                if (MatchResponse (
                                   cur,
                                   _expectedResponses,
                                   true,
                                   true))
                {
                    return false;
                }

                // Also try looking for late requests - in which case we do not invoke but still swallow content to avoid corrupting downstream
                if (MatchResponse (
                                   cur,
                                   _lateResponses,
                                   false,
                                   true))
                {
                    return false;
                }

                // Look for persistent requests
                if (MatchResponse (
                                   cur,
                                   _persistentExpectations,
                                   true,
                                   false))
                {
                    return false;
                }
            }

            // Finally if it is a valid ansi response but not one we are expect (e.g. its mouse activity)
            // then we can release it back to input processing stream
            if (_knownTerminators.Contains (cur.Last ()) && cur.StartsWith (EscSeqUtils.CSI))
            {
                // We have found a terminator so bail
                State = AnsiResponseParserState.Normal;

                // Maybe swallow anyway if user has custom delegate
                bool swallow = ShouldSwallowUnexpectedResponse ();

                if (swallow)
                {
                    _heldContent.ClearHeld ();

                    // Do not send back to input stream
                    return false;
                }

                // Do release back to input stream
                return true;
            }
        }

        return false; // Continue accumulating
    }

    /// <summary>
    ///     <para>
    ///         When overriden in a derived class, indicates whether the unexpected response
    ///         currently in <see cref="_heldContent"/> should be released or swallowed.
    ///         Use this to enable default event for escape codes.
    ///     </para>
    ///     <remarks>
    ///         Note this is only called for complete responses.
    ///         Based on <see cref="_knownTerminators"/>
    ///     </remarks>
    /// </summary>
    /// <returns></returns>
    protected abstract bool ShouldSwallowUnexpectedResponse ();

    private bool MatchResponse (string cur, List<AnsiResponseExpectation> collection, bool invokeCallback, bool removeExpectation)
    {
        // Check for expected responses
        AnsiResponseExpectation? matchingResponse = collection.FirstOrDefault (r => r.Matches (cur));

        if (matchingResponse?.Response != null)
        {
            if (invokeCallback)
            {
                matchingResponse.Response.Invoke (_heldContent);
            }

            ResetState ();

            if (removeExpectation)
            {
                collection.Remove (matchingResponse);
            }

            return true;
        }

        return false;
    }

    /// <inheritdoc/>
    public void ExpectResponse (string terminator, Action<string> response, Action? abandoned, bool persistent)
    {
        lock (_lockExpectedResponses)
        {
            if (persistent)
            {
                _persistentExpectations.Add (new (terminator, h => response.Invoke (h.HeldToString ()), abandoned));
            }
            else
            {
                _expectedResponses.Add (new (terminator, h => response.Invoke (h.HeldToString ()), abandoned));
            }
        }
    }

    /// <inheritdoc/>
    public bool IsExpecting (string terminator)
    {
        lock (_lockExpectedResponses)
        {
            // If any of the new terminator matches any existing terminators characters it's a collision so true.
            return _expectedResponses.Any (r => r.Terminator.Intersect (terminator).Any ());
        }
    }

    /// <inheritdoc/>
    public void StopExpecting (string terminator, bool persistent)
    {
        lock (_lockExpectedResponses)
        {
            if (persistent)
            {
                AnsiResponseExpectation [] removed = _persistentExpectations.Where (r => r.Matches (terminator)).ToArray ();

                foreach (AnsiResponseExpectation toRemove in removed)
                {
                    _persistentExpectations.Remove (toRemove);
                    toRemove.Abandoned?.Invoke ();
                }
            }
            else
            {
                AnsiResponseExpectation [] removed = _expectedResponses.Where (r => r.Terminator == terminator).ToArray ();

                foreach (AnsiResponseExpectation r in removed)
                {
                    _expectedResponses.Remove (r);
                    _lateResponses.Add (r);
                    r.Abandoned?.Invoke ();
                }
            }
        }
    }
}

internal class AnsiResponseParser<T> () : AnsiResponseParserBase (new GenericHeld<T> ())
{
    /// <inheritdoc cref="AnsiResponseParser.UnknownResponseHandler"/>
    public Func<IEnumerable<Tuple<char, T>>, bool> UnexpectedResponseHandler { get; set; } = _ => false;

    public IEnumerable<Tuple<char, T>> ProcessInput (params Tuple<char, T> [] input)
    {
        List<Tuple<char, T>> output = new ();

        ProcessInputBase (
                          i => input [i].Item1,
                          i => input [i],
                          c => output.Add ((Tuple<char, T>)c),
                          input.Length);

        return output;
    }

    public Tuple<char, T> [] Release ()
    {
        // Lock in case Release is called from different Thread from parse
        lock (_lockState)
        {
            Tuple<char, T> [] result = HeldToEnumerable ().ToArray ();

            ResetState ();

            return result;
        }
    }

    private IEnumerable<Tuple<char, T>> HeldToEnumerable () { return (IEnumerable<Tuple<char, T>>)_heldContent.HeldToObjects (); }

    /// <summary>
    ///     'Overload' for specifying an expectation that requires the metadata as well as characters. Has
    ///     a unique name because otherwise most lamdas will give ambiguous overload errors.
    /// </summary>
    /// <param name="terminator"></param>
    /// <param name="response"></param>
    /// <param name="abandoned"></param>
    /// <param name="persistent"></param>
    public void ExpectResponseT (string terminator, Action<IEnumerable<Tuple<char, T>>> response, Action? abandoned, bool persistent)
    {
        lock (_lockExpectedResponses)
        {
            if (persistent)
            {
                _persistentExpectations.Add (new (terminator, h => response.Invoke (HeldToEnumerable ()), abandoned));
            }
            else
            {
                _expectedResponses.Add (new (terminator, h => response.Invoke (HeldToEnumerable ()), abandoned));
            }
        }
    }

    /// <inheritdoc/>
    protected override bool ShouldSwallowUnexpectedResponse () { return UnexpectedResponseHandler.Invoke (HeldToEnumerable ()); }
}

internal class AnsiResponseParser () : AnsiResponseParserBase (new StringHeld ())
{
    /// <summary>
    ///     <para>
    ///         Delegate for handling unrecognized escape codes. Default behaviour
    ///         is to return <see langword="false"/> which simply releases the
    ///         characters back to input stream for downstream processing.
    ///     </para>
    ///     <para>
    ///         Implement a method to handle if you want and return <see langword="true"/> if you want the
    ///         keystrokes 'swallowed' (i.e. not returned to input stream).
    ///     </para>
    /// </summary>
    public Func<string, bool> UnknownResponseHandler { get; set; } = _ => false;

    public string ProcessInput (string input)
    {
        var output = new StringBuilder ();

        ProcessInputBase (
                          i => input [i],
                          i => input [i], // For string there is no T so object is same as char
                          c => output.Append ((char)c),
                          input.Length);

        return output.ToString ();
    }

    public string Release ()
    {
        lock (_lockState)
        {
            string output = _heldContent.HeldToString ();
            ResetState ();

            return output;
        }
    }

    /// <inheritdoc/>
    protected override bool ShouldSwallowUnexpectedResponse ()
    {
        lock (_lockState)
        {
            return UnknownResponseHandler.Invoke (_heldContent.HeldToString ());
        }
    }
}
