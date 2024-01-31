#region

using System.IO.Abstractions;
using System.Runtime.InteropServices;

#endregion

namespace Terminal.Gui {
    internal class AutocompleteFilepathContext : AutocompleteContext {
        public FileDialogState State { get; set; }

        public AutocompleteFilepathContext (string currentLine, int cursorPosition, FileDialogState state)
            : base (TextModel.ToRuneCellList (currentLine), cursorPosition) {
            this.State = state;
        }
    }

    internal class FilepathSuggestionGenerator : ISuggestionGenerator {
        FileDialogState state;

        public IEnumerable<Suggestion> GenerateSuggestions (AutocompleteContext context) {
            if (context is AutocompleteFilepathContext fileState) {
                this.state = fileState.State;
            }

            if (state == null) {
                return Enumerable.Empty<Suggestion> ();
            }

            var path = TextModel.ToString (context.CurrentLine);
            var last = path.LastIndexOfAny (FileDialog.Separators);

            if (string.IsNullOrWhiteSpace (path) || !Path.IsPathRooted (path)) {
                return Enumerable.Empty<Suggestion> ();
            }

            var term = path.Substring (last + 1);

            // If path is /tmp/ then don't just list everything in it
            if (string.IsNullOrWhiteSpace (term)) {
                return Enumerable.Empty<Suggestion> ();
            }

            if (term.Equals (state?.Directory?.Name)) {
                // Clear suggestions
                return Enumerable.Empty<Suggestion> ();
            }

            bool isWindows = RuntimeInformation.IsOSPlatform (OSPlatform.Windows);

            var suggestions = state.Children.Where (d => !d.IsParent)
                                   .Select (
                                            e => e.FileSystemInfo is IDirectoryInfo d
                                                     ? d.Name + Path.DirectorySeparatorChar
                                                     : e.FileSystemInfo.Name)
                                   .ToArray ();

            var validSuggestions = suggestions
                                   .Where (
                                           s => s.StartsWith (
                                                              term,
                                                              isWindows
                                                                  ? StringComparison.InvariantCultureIgnoreCase
                                                                  : StringComparison.InvariantCulture))
                                   .OrderBy (m => m.Length)
                                   .ToArray ();

            // nothing to suggest
            if (validSuggestions.Length == 0 || validSuggestions[0].Length == term.Length) {
                return Enumerable.Empty<Suggestion> ();
            }

            return validSuggestions.Select (
                                            f => new Suggestion (term.Length, f, f))
                                   .ToList ();
        }

        public bool IsWordChar (Rune rune) {
            if (rune.Value == '\n') {
                return false;
            }

            return true;
        }
    }
}
