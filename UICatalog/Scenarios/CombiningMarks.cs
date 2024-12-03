using Terminal.Gui;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Combining Marks", "Illustrates how Unicode Combining Marks work (or don't).")]
[ScenarioCategory ("Text and Formatting")]
public class CombiningMarks : Scenario
{
    public override void Main ()
    {
        Application.Init ();
        var top = new Toplevel ();

        top.DrawComplete += (s, e) =>
                                   {
                                       top.Move (0, 0);
                                       top.AddStr ("Terminal.Gui only supports combining marks that normalize. See Issue #2616.");
                                       top.Move (0, 2);
                                       top.AddStr ("\u0301\u0301\u0328<- \"\\u0301\\u0301\\u0328]\" using AddStr.");
                                       top.Move (0, 3);
                                       top.AddStr ("[a\u0301\u0301\u0328]<- \"[a\\u0301\\u0301\\u0328]\" using AddStr.");
                                       top.Move (0, 4);
                                       top.AddRune ('[');
                                       top.AddRune ('a');
                                       top.AddRune ('\u0301');
                                       top.AddRune ('\u0301');
                                       top.AddRune ('\u0328');
                                       top.AddRune (']');
                                       top.AddStr ("<- \"[a\\u0301\\u0301\\u0328]\" using AddRune for each.");
                                       top.Move (0, 6);
                                       top.AddStr ("[e\u0301\u0301\u0328]<- \"[e\\u0301\\u0301\\u0328]\" using AddStr.");
                                       top.Move (0, 7);
                                       top.AddStr ("[e\u0301\u0328\u0301]<- \"[e\\u0301\\u0328\\u0301]\" using AddStr.");
                                       top.Move (0, 8);
                                       top.AddStr ("[e\u0328\u0301]<- \"[e\\u0328\\u0301]\" using AddStr.");
                                       top.Move (0, 10);
                                       top.AddStr ("From now on we are using Text Formatter");
                                       TextFormatter tf = new () { Text = "[e\u0301\u0301\u0328]<- \"[e\\u0301\\u0301\\u0328]\" using TextFormatter." };
                                       tf.Draw (new (0, 11, tf.Text.Length, 1), top.ColorScheme.Normal, top.ColorScheme.Normal);
                                       tf.Text = "[e\u0328\u0301]<- \"[e\\u0328\\u0301]\" using TextFormatter.";
                                       tf.Draw (new (0, 12, tf.Text.Length, 1), top.ColorScheme.Normal, top.ColorScheme.Normal);
                                   };

        Application.Run (top);
        top.Dispose ();
        Application.Shutdown ();
    }
}
