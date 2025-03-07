using System.Reflection;
using UnitTests;
using Xunit.Abstractions;

namespace Terminal.Gui.ViewsTests;

public class AllViewsTests (ITestOutputHelper output) : TestsAllViews
{
    [Theory]
    [MemberData (nameof (AllViewTypes))]
    public void AllViews_Tests_All_Constructors (Type viewType)
    {
        Assert.True (TestAllConstructorsOfType (viewType));

        return;

        bool TestAllConstructorsOfType (Type type)
        {
            foreach (ConstructorInfo ctor in type.GetConstructors ())
            {
                View view = CreateViewFromType (type, ctor);

                if (view != null)
                {
                    Assert.True (type.FullName == view.GetType ().FullName);
                }
            }

            return true;
        }
    }
}
