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

        // Mirror Python behavior: initialize the mixture and adopt T/P from the first phase
        // Python's Mixture.__cinit__ calls mix.init() and sets P and T to the first phase's values
        LibCantera.mix_init(_mix);

        if (phases.Length > 0)
        {
            var (firstPhase, _) = phases[0];
            // Set pressure before temperature (order shouldn't matter, but stays consistent)
            LibCantera.mix_setPressure(_mix, firstPhase.Pressure);
            LibCantera.mix_setTemperature(_mix, firstPhase.Temperature);
        }
    }

    // Align with Python Mixture.equilibrate accepted pairs: ["TP", "HP", "SP"]
    private static readonly string[] ALLOWED_XY_VALUES = ["TP", "HP", "SP"];

    /// <summary>
    /// Expose key thermodynamic getters for debugging/analysis parity.
    /// </summary>
    /// <remarks>All units follow Cantera conventions: T [K], P [Pa], H/S/G [J], Cp [J/K], Volume [m^3].</remarks>
    /// <value>Current mixture temperature in Kelvin.</value>
    public double Temperature => LibCantera.mix_temperature(_mix);
    /// <summary>Current mixture pressure in Pascal.</summary>
    public double Pressure => LibCantera.mix_pressure(_mix);
    /// <summary>Total mixture enthalpy [J].</summary>
    public double Enthalpy => LibCantera.mix_enthalpy(_mix);
    /// <summary>Total mixture entropy [J/K].</summary>
    public double Entropy => LibCantera.mix_entropy(_mix);
    /// <summary>Total mixture Gibbs free energy [J].</summary>
    public double Gibbs => LibCantera.mix_gibbs(_mix);
    /// <summary>Total mixture isobaric heat capacity Cp [J/K].</summary>
    public double Cp => LibCantera.mix_cp(_mix);
    /// <summary>Total mixture volume [m^3].</summary>
    public double Volume => LibCantera.mix_volume(_mix);
    /// <summary>Number of elements present in the mixture.</summary>
    public int ElementCount => LibCantera.mix_nElements(_mix);
    /// <summary>Get total moles of an element by element index.</summary>
    /// <param name="elementIndex">Element index (0-based).</param>
    /// <returns>Moles of the element across all species.</returns>
    public double GetElementMoles(int elementIndex) => LibCantera.mix_elementMoles(_mix, elementIndex);

    /// <summary>
    /// TODO
    /// </summary>
    public void Equilibrate(string XY, string solver = "auto", double rtol = 1e-9, int maxSteps = 1000, int maxIterations = 100, int estimateEquil = 0)
    {
        ArgumentNullException.ThrowIfNull(XY);

        var xy = XY.ToUpperInvariant();

        if (Array.IndexOf(ALLOWED_XY_VALUES, xy) < 0)
        {
            throw new ArgumentException($"XY can only be {string.Join(',', ALLOWED_XY_VALUES)}");
        }

        // Normalize and validate solver; pass lowercase to native API explicitly
        var sUpper = (solver ?? "auto").ToUpperInvariant();
        if (sUpper != "AUTO" && sUpper != "VCS" && sUpper != "GIBBS")
        {
            throw new ArgumentException("solver must be one of: 'auto', 'vcs', 'gibbs'", nameof(solver));
        }
        var sLower = sUpper switch { "AUTO" => "auto", "VCS" => "vcs", "GIBBS" => "gibbs", _ => "auto" };

        LibCantera.mix_equilibrate(_mix, xy, solver: sLower, rtol, maxSteps, maxIterations, estimateEquil);
    }
}
