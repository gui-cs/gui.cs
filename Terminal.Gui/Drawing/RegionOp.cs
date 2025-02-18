#nullable enable
namespace Terminal.Gui;

/// <summary>
/// Specifies the operation to perform when combining regions.
/// </summary>
public enum RegionOp
{
    /// <summary>
    ///     Subtract the op region from the first region.
    /// </summary>
    Difference = 0,

    /// <summary>
    ///     Intersect the two regions.
    /// </summary>
    Intersect = 1,

    /// <summary>
    ///    Union (inclusive-or) the two regions.
    /// </summary>
    Union = 2,

    /// <summary>
    ///   Exclusive-or the two regions.
    /// </summary>
    XOR = 3,

    /// <summary>
    ///   Subtract the first region from the op region.
    /// </summary>
    ReverseDifference = 4,

    /// <summary>
    /// 
    /// </summary>
    Replace = 5
}
