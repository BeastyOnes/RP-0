﻿using System.Collections.Generic;
using ToolbarControl_NS;
using Upgradeables;

namespace KerbalConstructionTime
{
    public static class KCTGameStates
    {
        internal const string _modId = "KCT_NS";
        internal const string _modName = "Kerbal Construction Time";

        public static KCTSettings Settings = new KCTSettings();

        public static KSCItem ActiveKSC = null;
        public static List<KSCItem> KSCs = new List<KSCItem>();
        public static string ActiveKSCName = string.Empty;
        public static float SciPointsTotal = -1f;
        public static bool MergingAvailable;
        public static List<BuildListVessel> MergedVessels = new List<BuildListVessel>();

        public static KCTObservableList<TechItem> TechList = new KCTObservableList<TechItem>();
        public static void UpdateTechTimes()
        {
            for (int j = 0; j < TechList.Count; j++)
                TechList[j].UpdateBuildRate(j);
        }

        //public static List<int> PurchasedUpgrades = new List<int>() { 0, 0 };
        //public static int MiscellaneousTempUpgrades = 0, LastKnownTechCount = 0;
        public static int UnassignedPersonnel = 0;
        public static int Researchers = 0;
        public static double LastResearchers = 0;
        private const double StartingEfficiencyEngineers = 0.25d;
        public static double EfficiencyEngineers = StartingEfficiencyEngineers;
        public static double LastEngineers = 0;
        public static BuildListVessel LaunchedVessel, EditedVessel, RecoveredVessel;
        public static List<PartCrewAssignment> LaunchedCrew = new List<PartCrewAssignment>();
        public static int LoadedSaveVersion = 0;
        public const int VERSION = 5;

        public static ToolbarControl ToolbarControl;

        public static bool EditorShipEditingMode = false;
        public static bool IsFirstStart = false;
        public static bool StarterLCBuilding = false;
        public static bool HiredStarterApplicants = false;
        public static bool StartedProgram = false;
        public static bool AcceptedContract = false;
        public static bool FirstRunNotComplete => !(StarterLCBuilding && HiredStarterApplicants && StartedProgram && AcceptedContract);
        public static bool IsSimulatedFlight = false;
        public static double EditorBuildPoints = 0;
        public static double EditorShipMass = 0;
        public static UnityEngine.Vector3 EditorShipSize = UnityEngine.Vector3.zero;
        public static bool EditorIsHumanRated = false;
        public static double EditorIntegrationPoints = 0;
        public static double EditorRolloutCosts = 0;
        public static double EditorRolloutTime = 0;
        public static double EditorIntegrationCosts = 0;
        public static double EditorUnlockCosts = 0;
        public static List<string> EditorRequiredTechs = new List<string>();

        public static Dictionary<string, int> BuildingMaxLevelCache = new Dictionary<string, int>();

        public static List<bool> ShowWindows = new List<bool> { false, true };    //build list, editor
        public static string KACAlarmId = string.Empty;
        public static double KACAlarmUT = 0;

        public static bool ErroredDuringOnLoad = false;

        public static bool VesselErrorAlerted = false;
        public static bool PersistenceLoaded = false;
        public static bool IsRefunding = false;

        public static AirlaunchParams AirlaunchParams;
        public static SimulationParams SimulationParams = new SimulationParams();

        public static void Reset()
        {
            IsFirstStart = false;
            StarterLCBuilding = false;
            HiredStarterApplicants = false;
            StartedProgram = false;
            VesselErrorAlerted = false;

            IsSimulatedFlight = false;
            SimulationParams.Reset();

            AirlaunchParams = null;

            KCT_GUI.ResetFormulaRateHolders();
            KCT_GUI.ResetShowFirstRunAgain();

            BuildingMaxLevelCache.Clear();

            InitAndClearTechList();

            UnassignedPersonnel = 0;
            Researchers = 0;
            LastResearchers = 0;
            EfficiencyEngineers = StartingEfficiencyEngineers;
            LastEngineers = 0;
            LoadedSaveVersion = 0;
        }

        public static void InitAndClearTechList()
        {
            TechList = new KCTObservableList<TechItem>();
            if (KerbalConstructionTime.Instance != null)    // Can be null/destroyed in the main menu scene
            {
                TechList.Updated += KerbalConstructionTime.Instance.ForceUpdateRndScreen;
            }

            void updated() { KCTEvents.OnRP0MaintenanceChanged.Fire(); }
            TechList.Updated += updated;
        }

        public static void ClearVesselEditMode()
        {
            EditorShipEditingMode = false;
            EditedVessel = null;
            MergedVessels.Clear();

            InputLockManager.RemoveControlLock("KCTEditExit");
            InputLockManager.RemoveControlLock("KCTEditLoad");
            InputLockManager.RemoveControlLock("KCTEditNew");
            InputLockManager.RemoveControlLock("KCTEditLaunch");
            EditorLogic.fetch?.Unlock("KCTEditorMouseLock");
        }

        public static void ClearLaunchpadList()
        {
            ActiveKSC.ActiveLaunchComplexInstance.LaunchPads.Clear();
        }

        public static LCItem FindLCFromID(System.Guid guid)
        {
            foreach (var ksc in KSCs)
                foreach (LCItem lc in ksc.LaunchComplexes)
                    if (lc.ID == guid)
                        return lc;

            return null;
        }

        public static void RecalculateBuildRates()
        {
            foreach (var ksc in KSCs)
                ksc.RecalculateBuildRates(true);

            for (int i = TechList.Count; i-- > 0;)
            {
                TechItem tech = KCTGameStates.TechList[i];
                tech.UpdateBuildRate(i);
            }
        }

        public static int GetSalaryEngineers() => (int)(-RP0.CurrencyUtils.Funds(RP0.TransactionReasonsRP0.SalaryEngineers, -RP0.MaintenanceHandler.Instance.IntegrationSalaryPerDay * 365.25d));
        public static int GetSalaryResearchers() => (int)(-RP0.CurrencyUtils.Funds(RP0.TransactionReasonsRP0.SalaryResearchers, -RP0.MaintenanceHandler.Instance.ResearchSalaryPerDay * 365.25d));
        public static int GetTotalSalary() => GetSalaryEngineers() + GetSalaryResearchers();

        public static double GetEffectiveIntegrationEngineersForSalary(KSCItem ksc)
        {
            double engineers = 0d;
            foreach (var lc in ksc.LaunchComplexes)
                engineers += GetEffectiveEngineersForSalary(lc);
            return engineers + ksc.UnassignedEngineers * PresetManager.Instance.ActivePreset.GeneralSettings.IdleSalaryMult;
        }

        public static double GetEffectiveEngineersForSalary(KSCItem ksc) => GetEffectiveIntegrationEngineersForSalary(ksc);

        public static double GetEffectiveEngineersForSalary(LCItem lc)
        {
            if (lc.IsOperational && lc.Engineers > 0)
            {
                if (lc.IsIdle) // not IsActive because completed rollouts/airlaunches still count
                    return lc.Engineers * PresetManager.Instance.ActivePreset.GeneralSettings.IdleSalaryMult;

                if (lc.IsHumanRated && lc.BuildList.Count > 0 && !lc.BuildList[0].IsHumanRated)
                {
                    int num = System.Math.Min(lc.Engineers, lc.MaxEngineersFor(lc.BuildList[0]));
                    return num * lc.RushSalary + (lc.Engineers - num) * PresetManager.Instance.ActivePreset.GeneralSettings.IdleSalaryMult;
                }

                return lc.Engineers * lc.RushSalary;
            }

            return 0;
        }

        public static double GetBudgetDelta(double time)
        {
            // note NetUpkeepPerDay is negative or 0.
            
            double subsidy = RP0.CurrencyUtils.Funds(RP0.TransactionReasonsRP0.Subsidy, RP0.MaintenanceHandler.Instance.MaintenanceSubsidyPerDay);
            double upkeep = RP0.MaintenanceHandler.Instance.NetUpkeepPerDay - subsidy;
            double averageSubsidy = subsidy * 0.5d + RP0.CurrencyUtils.Funds(RP0.TransactionReasonsRP0.Subsidy, RP0.MaintenanceHandler.GetSubsidyAt(RP0.KSPUtils.GetUT() + time)) * 0.5d;
            double delta = (RP0.MaintenanceHandler.Instance.NetUpkeepPerDay + averageSubsidy ) * time / 86400d + GetConstructionCostOverTime(time) + GetRolloutCostOverTime(time) + GetAirlaunchCostOverTime(time);
            delta += RP0.MaintenanceHandler.Instance.GetDisplayProgramFunding(time);

            return delta;
        }

        public static double GetConstructionCostOverTime(double time)
        {
            double delta = 0;
            foreach (var ksc in KSCs)
            {
                delta += GetConstructionCostOverTime(time, ksc);
            }
            return delta;
        }

        public static double GetConstructionCostOverTime(double time, KSCItem ksc)
        {
            double delta = 0;
            foreach (var c in ksc.Constructions)
                delta += c.GetConstructionCostOverTime(time);

            return delta;
        }

        public static double GetConstructionCostOverTime(double time, string kscName)
        {
            foreach (var ksc in KSCs)
            {
                if (ksc.KSCName == kscName)
                {
                    return GetConstructionCostOverTime(time, ksc);
                }
            }

            return 0d;
        }

        public static double GetRolloutCostOverTime(double time)
        {
            double delta = 0;
            foreach (var ksc in KSCs)
            {
                delta += GetRolloutCostOverTime(time, ksc);
            }
            return delta;
        }

        public static double GetRolloutCostOverTime(double time, KSCItem ksc)
        {
            double delta = 0;
            for(int i = 1; i < ksc.LaunchComplexes.Count; ++i)
                delta += GetRolloutCostOverTime(time, ksc.LaunchComplexes[i]);

            return delta;
        }

        public static double GetRolloutCostOverTime(double time, LCItem lc)
        {
            double delta = 0;
            foreach (var rr in lc.Recon_Rollout)
            {
                if (rr.RRType != ReconRollout.RolloutReconType.Rollout)
                    continue;
                
                double t = rr.GetTimeLeft();
                double fac = 1d;
                if (t > time)
                    fac = time / t;

                delta += RP0.CurrencyUtils.Funds(RP0.TransactionReasonsRP0.RocketRollout, -rr.Cost * (1d - rr.Progress / rr.BP) * fac);
            }

            return delta;
        }

        public static double GetAirlaunchCostOverTime(double time)
        {
            double delta = 0;
            foreach (var ksc in KSCs)
            {
                delta += GetAirlaunchCostOverTime(time, ksc);
            }
            return delta;
        }

        public static double GetAirlaunchCostOverTime(double time, KSCItem ksc)
        {
            double delta = 0;
            foreach (var al in ksc.Hangar.AirlaunchPrep)
            {
                if (al.Direction == AirlaunchPrep.PrepDirection.Mount)
                {
                    double t = al.GetTimeLeft();
                    double fac = 1d;
                    if (t > time)
                        fac = time / t;

                    delta += RP0.CurrencyUtils.Funds(RP0.TransactionReasonsRP0.AirLaunchRollout, -al.Cost * (1d - al.Progress / al.BP) * fac);
                }
            }

            return delta;
        }

        public static double GetRolloutCostOverTime(double time, string kscName)
        {
            foreach (var ksc in KSCs)
            {
                if (ksc.KSCName == kscName)
                {
                    return GetRolloutCostOverTime(time, ksc);
                }
            }

            return 0d;
        }

        public static int TotalEngineers
        {
            get
            {
                int eng = 0;
                foreach (var ksc in KCTGameStates.KSCs)
                    eng += ksc.Engineers;

                return eng;
            }
        }
    }
}

/*
    KerbalConstructionTime (c) by Michael Marvin, Zachary Eck

    KerbalConstructionTime is licensed under a
    Creative Commons Attribution-NonCommercial-ShareAlike 4.0 International License.

    You should have received a copy of the license along with this
    work. If not, see <http://creativecommons.org/licenses/by-nc-sa/4.0/>.
*/
