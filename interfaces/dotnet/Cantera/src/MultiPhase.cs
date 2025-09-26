// This file is part of Cantera. See License.txt in the top-level directory or
// at https://cantera.org/license.txt for license and copyright information.

using Cantera.Interop;

namespace Cantera;

/// <summary>
/// Represents a thermodynamic phase.
/// </summary>
public partial class MultiPhase
{
    readonly MixtureHandle _mix;

    internal MultiPhase(params (ThermoPhase phase, double moles)[] phases)
    {
        _mix = LibCantera.mix_new();

        foreach (var (phase, moles) in phases)
        {
            LibCantera.mix_addPhase(_mix, phase.GetPointer(), moles);
        }
    }

    private static readonly string[] ALLOWED_XY_VALUES = ["TP", "HP", "TV"];

    /// <summary>
    /// TODO
    /// </summary>
    public void Equilibrate(string XY, double rtol = 1e-9, int maxSteps = 5000, int maxIterations = 100, int estimateEquil = 0)
    {
        if (!ALLOWED_XY_VALUES.Contains(XY))
        {
            throw new ArgumentException($"XY can only be {string.Join(',', ALLOWED_XY_VALUES)}");
        }

        LibCantera.mix_equilibrate(_mix, XY, solver: "auto", rtol, maxSteps, maxIterations, estimateEquil);
    }
}
