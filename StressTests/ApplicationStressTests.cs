using Terminal.Gui;

namespace StressTests;

public class ApplicationStressTests : TestsAllViews
{
    private static volatile int _tbCounter;
    private static readonly ManualResetEventSlim _wakeUp = new (false);

    [Theory]
    [InlineData (typeof (FakeDriver))]
    [InlineData (typeof (NetDriver), Skip = "System.IO.IOException: The handle is invalid")]
    //[InlineData (typeof (ANSIDriver))]
    [InlineData (typeof (WindowsDriver), Skip = "Increment lost. tbCounter (25500) didn't change after waiting 100 ms. Failed to reach 500 on pass 1")]
    [InlineData (typeof (CursesDriver), Skip = "Unable to load DLL 'libc' or one of its dependencies: The specified module could not be found. (0x8007007E)")]
    public async Task InvokeLeakTest (Type driverType)
    {
        Application.Init (driverName: driverType.Name);
        Random r = new ();
        TextField tf = new ();
        var top = new Toplevel ();
        top.Add (tf);

        const int NUM_PASSES = 50;
        const int NUM_INCREMENTS = 500;
        const int POLL_MS = 100;

        Task task = Task.Run (() => RunTest (r, tf, NUM_PASSES, NUM_INCREMENTS, POLL_MS));

        // blocks here until the RequestStop is processed at the end of the test
        Application.Run (top);

        await task; // Propagate exception if any occurred

        Assert.Equal (NUM_INCREMENTS * NUM_PASSES, _tbCounter);
        top.Dispose ();
        Application.Shutdown ();

        return;

        static void RunTest (Random r, TextField tf, int numPasses, int numIncrements, int pollMs)
        {
            for (var j = 0; j < numPasses; j++)
            {
                _wakeUp.Reset ();

                for (var i = 0; i < numIncrements; i++)
                {
                    Launch (r, tf, (j + 1) * numIncrements);
                }

                while (_tbCounter != (j + 1) * numIncrements) // Wait for tbCounter to reach expected value
                {
                    int tbNow = _tbCounter;
                    _wakeUp.Wait (pollMs);

                    if (_tbCounter != tbNow)
                    {
                        continue;
                    }

                    // No change after wait: Idle handlers added via Application.Invoke have gone missing
                    Application.Invoke (() => Application.RequestStop ());

                    throw new TimeoutException (
                                                $"Timeout: Increment lost. tbCounter ({_tbCounter}) didn't "
                                                + $"change after waiting {pollMs} ms. Failed to reach {(j + 1) * numIncrements} on pass {j + 1}"
                                               );
                }

                ;
            }

            Application.Invoke (() => Application.RequestStop ());
        }

        static void Launch (Random r, TextField tf, int target)
        {
            Task.Run (
                      () =>
                      {
                          Thread.Sleep (r.Next (2, 4));

                          Application.Invoke (
                                              () =>
                                              {
                                                  tf.Text = $"index{r.Next ()}";
                                                  Interlocked.Increment (ref _tbCounter);

                                                  if (target == _tbCounter)
                                                  {
                                                      // On last increment wake up the check
                                                      _wakeUp.Set ();
                                                  }
                                              }
                                             );
                      }
                     );
        }
    }
}
