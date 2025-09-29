// This file is part of Cantera. See License.txt in the top-level directory or
// at https://cantera.org/license.txt for license and copyright information.

using System.Diagnostics.CodeAnalysis;
using Cantera.Interop;

namespace Cantera;

/// <summary>
/// Represents a thermodynamic phase.
/// </summary>
public partial class ThermoPhase
{
    [SuppressMessage("Usage", "CA2213: Disposable field not disposed",
        Justification = "Field actually is disposed, in ExtraDispose()")]
    readonly SolutionHandle _sol;

    readonly Lazy<SpeciesCollection> _species;

    /// <summary>
    /// The collection of species that make up this phase.
    /// </summary>
    public SpeciesCollection Species => _species.Value;

    internal ThermoPhase(string filename, string? phaseName)
    {
        _sol = LibCantera.sol_newSolution(filename, phaseName ?? "", "none");
        _handle = LibCantera.sol_thermo(_sol);

        _species = new(() => new SpeciesCollection(_handle));
    }

    /// <summary>
    /// Simulates bringing the phase to thermodynamic equilibrium by holding the
    /// specified <see cref="ThermoPair" /> constant and using the algorithm(s)
    /// identified by the solver string.
    /// </summary>
    public void Equilibrate(ThermoPair thermoPair,
                            string solver = "auto",
                            double tolerance = 1e-9, int maxSteps = 1000,
                            int maxIterations = 100, int logVerbosity = 0)
    {
        var interopString = thermoPair.ToInteropString();

        LibCantera.thermo_equilibrate(_handle, interopString, solver,
            tolerance, maxSteps, maxIterations, logVerbosity);
    }

    /// <summary>
    /// Sets the given pair of thermodynamic properties for this phase together.
    /// </summary>
    public void SetPair(ThermoPair pair, double first, double second) =>
        _ = pair switch
        {
            ThermoPair.DP => LibCantera.thermo_setState_DP(_handle, first, second),
            ThermoPair.TV => LibCantera.thermo_setState_TV(_handle, first, second),
            ThermoPair.HP => LibCantera.thermo_setState_HP(_handle, first, second),
            ThermoPair.SP => LibCantera.thermo_setState_SP(_handle, first, second),
            ThermoPair.PV => LibCantera.thermo_setState_PV(_handle, first, second),
            ThermoPair.TP => LibCantera.thermo_setState_TP(_handle, first, second),
            ThermoPair.UV => LibCantera.thermo_setState_UV(_handle, first, second),
            ThermoPair.ST => LibCantera.thermo_setState_ST(_handle, first, second),
            ThermoPair.SV => LibCantera.thermo_setState_SV(_handle, first, second),
            ThermoPair.UP => LibCantera.thermo_setState_UP(_handle, first, second),
            ThermoPair.VH => LibCantera.thermo_setState_VH(_handle, first, second),
            ThermoPair.TH => LibCantera.thermo_setState_TH(_handle, first, second),
            ThermoPair.SH => LibCantera.thermo_setState_SH(_handle, first, second),
            _ => throw new ArgumentOutOfRangeException(nameof(pair))
        };

    /// <summary>
    /// Gets the pointer to the solution
    /// </summary>
    public int GetPointer() => _sol.GetPointer();

    /// <summary>
    /// Gets the moles for each species (mole fractions * total moles).
    /// Equivalent to Python's: gas.X * (1.0 / gas.mean_molecular_weight)
    /// </summary>
    public double[] GetMoles()
    {
        double totMoles = 1.0 / MeanMolecularWeight;
        double[] moleFractions = LibCantera.thermo_getMoleFractions(_handle);

        double[] moles = new double[moleFractions.Length];
        for (int i = 0; i < moleFractions.Length; i++)
        {
            moles[i] = moleFractions[i] * totMoles;
        }

        return moles;
    }

    /// <summary>
    /// Gets the number of elements in the phase.
    /// Equivalent to Python's: gas.n_elements
    /// </summary>
    public int NElements => LibCantera.thermo_nElements(_handle);

    /// <summary>
    /// Gets the number of species in the phase.
    /// Equivalent to Python's: gas.n_species
    /// </summary>
    public int NSpecies => LibCantera.thermo_nSpecies(_handle);

    /// <summary>
    /// Gets the element names.
    /// Equivalent to Python's: gas.element_names
    /// </summary>
    public string[] ElementNames
    {
        get
        {
            int nElements = NElements;
            string[] names = new string[nElements];
            for (int i = 0; i < nElements; i++)
            {
                names[i] = LibCantera.thermo_elementName(_handle, i);
            }
            return names;
        }
    }

    /// <summary>
    /// Gets the species names.
    /// Equivalent to Python's: gas.species_names
    /// </summary>
    public string[] SpeciesNames
    {
        get
        {
            int nSpecies = NSpecies;
            string[] names = new string[nSpecies];
            for (int i = 0; i < nSpecies; i++)
            {
                names[i] = LibCantera.thermo_speciesName(_handle, i);
            }
            return names;
        }
    }

    /// <summary>
    /// Gets the number of atoms of element m in species k.
    /// Equivalent to Python's: gas.n_atoms(species, element)
    /// </summary>
    /// <param name="speciesIndex">Species index</param>
    /// <param name="elementIndex">Element index</param>
    /// <returns>Number of atoms</returns>
    public double GetNAtoms(int speciesIndex, int elementIndex) =>
        LibCantera.thermo_nAtoms(_handle, speciesIndex, elementIndex);

    /// <summary>
    /// Gets the number of atoms of element in species by name.
    /// Equivalent to Python's: gas.n_atoms(species_name, element_name)
    /// </summary>
    /// <param name="speciesName">Species name</param>
    /// <param name="elementName">Element name</param>
    /// <returns>Number of atoms</returns>
    public double GetNAtoms(string speciesName, string elementName)
    {
        int speciesIndex = LibCantera.thermo_speciesIndex(_handle, speciesName);
        int elementIndex = LibCantera.thermo_elementIndex(_handle, elementName);
        return LibCantera.thermo_nAtoms(_handle, speciesIndex, elementIndex);
    }

    /// <summary>
    /// Gets the standard enthalpies divided by RT for each species.
    /// Equivalent to Python's: gas.standard_enthalpies_RT
    /// </summary>
    public double[] StandardEnthalpiesRT
    {
        get
        {
            int nSpecies = NSpecies;
            double[] chemPotentials = new double[nSpecies];
            LibCantera.thermo_getChemPotentials(_handle, chemPotentials);

            // Convert chemical potentials to standard enthalpies/RT
            // μ/RT = H°/RT - S°/RT
            // For ideal gas at standard state, we can derive H°/RT from partial molar enthalpies
            double[] partialMolarEnthalpies = new double[nSpecies];
            LibCantera.thermo_getPartialMolarEnthalpies(_handle, partialMolarEnthalpies);

            double[] standardEnthalpiesRT = new double[nSpecies];
            double RT = 8314.462618 * Temperature; // R * T (J/mol)

            for (int i = 0; i < nSpecies; i++)
            {
                standardEnthalpiesRT[i] = partialMolarEnthalpies[i] / RT;
            }

            return standardEnthalpiesRT;
        }
    }

    partial void ExtraDispose()
    {
        _sol.Dispose();
    }
}
