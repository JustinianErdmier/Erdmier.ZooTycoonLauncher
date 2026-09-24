namespace Erdmier.ZooTycoonLauncher.Desktop.Models;

/// <summary>Immutable row projection used by <c>InstallationGridViewModel</c> to populate the installation <c>DataGrid</c>.</summary>
/// <param name="Id">The installation's identifier.</param>
/// <param name="Name">The user-visible installation name.</param>
/// <param name="Path">The fully qualified directory path.</param>
/// <param name="ValidityDisplayName">Human-readable validity label (e.g. <c>Valid</c>, <c>Invalid — No EXE</c>).</param>
/// <param name="ValidityColourToken">Colour token consumed by <c>ColourTokenToBrushConverter</c> — <c>Green</c> for valid, <c>Red</c> for all invalid states.</param>
/// <param name="IsDefault"><see langword="true" /> when this installation is the launcher default; drives bold font weight on the Name cell.</param>
public sealed record InstallationGridRowModel(Guid   Id,
                                              string Name,
                                              string Path,
                                              string ValidityDisplayName,
                                              string ValidityColourToken,
                                              bool   IsDefault);
