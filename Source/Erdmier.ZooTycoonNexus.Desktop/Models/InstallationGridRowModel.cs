namespace Erdmier.ZooTycoonNexus.Desktop.Models;

/// <summary>Immutable row projection used by <c>InstallationGridViewModel</c> to populate the installation <c>DataGrid</c>.</summary>
/// <param name="Id">The installation's identifier.</param>
/// <param name="Name">The user-visible installation name.</param>
/// <param name="Path">The fully qualified directory path.</param>
/// <param name="ValidityDisplayName">Human-readable validity label (e.g. <c>Valid</c>, <c>Invalid — No EXE</c>).</param>
/// <param name="Validity">The installation's validity; the row template maps it to a status colour with <c>ValidityToBrushConverter</c>.</param>
/// <param name="IsDefault"><see langword="true" /> when this installation is the launcher default; drives bold font weight on the Name cell.</param>
public sealed record InstallationGridRowModel(Guid   Id,
                                              string Name,
                                              string Path,
                                              string ValidityDisplayName,
                                              InstallationValidity Validity,
                                              bool   IsDefault);
