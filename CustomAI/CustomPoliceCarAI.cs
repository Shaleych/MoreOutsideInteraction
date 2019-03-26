﻿using ColossalFramework;
using ColossalFramework.Math;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace MoreOutsideInteraction.CustomAI
{
    public class CustomPoliceCarAI : PoliceCarAI
    {
        private bool ArriveAtTarget(ushort vehicleID, ref Vehicle data)
        {
            if (data.m_targetBuilding == 0)
            {
                return true;
            }

            if (Singleton<BuildingManager>.instance.m_buildings.m_buffer[(int)data.m_targetBuilding].m_flags.IsFlagSet(Building.Flags.Untouchable))
            {
                int num = Mathf.Min(0, (int)data.m_transferSize - this.m_criminalCapacity);
                BuildingInfo info = Singleton<BuildingManager>.instance.m_buildings.m_buffer[(int)data.m_targetBuilding].Info;
                info.m_buildingAI.ModifyMaterialBuffer(data.m_targetBuilding, ref Singleton<BuildingManager>.instance.m_buildings.m_buffer[(int)data.m_targetBuilding], (TransferManager.TransferReason)data.m_transferType, ref num);
                var instance = Singleton<BuildingManager>.instance;
                if ((instance.m_buildings.m_buffer[(int)data.m_targetBuilding].m_flags & Building.Flags.IncomingOutgoing) == Building.Flags.Incoming)
                {
                    if (Loader.isRealCityRunning)
                    {
                        double x = instance.m_buildings.m_buffer[(int)data.m_targetBuilding].m_position.x - instance.m_buildings.m_buffer[(int)data.m_sourceBuilding].m_position.x;
                        double z = instance.m_buildings.m_buffer[(int)data.m_targetBuilding].m_position.z - instance.m_buildings.m_buffer[(int)data.m_sourceBuilding].m_position.z;
                        x = (x > 0) ? x : -x;
                        z = (z > 0) ? z : -z;
                        double distance = (x + z) / 2f;
                        Singleton<EconomyManager>.instance.AddPrivateIncome((int)(-num * (distance * 2f)), ItemClass.Service.PoliceDepartment, ItemClass.SubService.None, ItemClass.Level.Level3, 115);
                    }
                    ushort num3 = instance.FindBuilding(instance.m_buildings.m_buffer[(int)data.m_targetBuilding].m_position, 200f, info.m_class.m_service, ItemClass.SubService.None, Building.Flags.Outgoing, Building.Flags.Incoming);
                    if (num3 != 0)
                    {
                        BuildingInfo info3 = instance.m_buildings.m_buffer[(int)num3].Info;
                        Randomizer randomizer = new Randomizer((int)vehicleID);
                        Vector3 vector;
                        Vector3 vector2;
                        info3.m_buildingAI.CalculateSpawnPosition(num3, ref instance.m_buildings.m_buffer[(int)num3], ref randomizer, this.m_info, out vector, out vector2);
                        Quaternion rotation = Quaternion.identity;
                        Vector3 forward = vector2 - vector;
                        if (forward.sqrMagnitude > 0.01f)
                        {
                            rotation = Quaternion.LookRotation(forward);
                        }
                        data.m_frame0 = new Vehicle.Frame(vector, rotation);
                        data.m_frame1 = data.m_frame0;
                        data.m_frame2 = data.m_frame0;
                        data.m_frame3 = data.m_frame0;
                        data.m_targetPos0 = vector;
                        data.m_targetPos0.w = 2f;
                        data.m_targetPos1 = vector2;
                        data.m_targetPos1.w = 2f;
                        data.m_targetPos2 = data.m_targetPos1;
                        data.m_targetPos3 = data.m_targetPos1;
                        this.FrameDataUpdated(vehicleID, ref data, ref data.m_frame0);
                        this.SetTarget(vehicleID, ref data, 0);
                        return false;
                    }
                }
            }
            else
            {

                if (this.m_info.m_class.m_level >= ItemClass.Level.Level4)
                {
                    this.ArrestCriminals(vehicleID, ref data, data.m_targetBuilding);
                    data.m_flags |= Vehicle.Flags.Stopped;
                }
                else
                {
                    int num = -this.m_crimeCapacity;
                    BuildingInfo info = Singleton<BuildingManager>.instance.m_buildings.m_buffer[(int)data.m_targetBuilding].Info;
                    info.m_buildingAI.ModifyMaterialBuffer(data.m_targetBuilding, ref Singleton<BuildingManager>.instance.m_buildings.m_buffer[(int)data.m_targetBuilding], (TransferManager.TransferReason)data.m_transferType, ref num);
                    if ((data.m_flags & Vehicle.Flags.Emergency2) != (Vehicle.Flags)0)
                    {
                        this.ArrestCriminals(vehicleID, ref data, data.m_targetBuilding);
                        for (int i = 0; i < this.m_policeCount; i++)
                        {
                            this.CreatePolice(vehicleID, ref data, Citizen.AgePhase.Adult0);
                        }
                        data.m_flags |= Vehicle.Flags.Stopped;
                    }
                }
            }
            this.SetTarget(vehicleID, ref data, 0);
            return false;
        }

        private void ArrestCriminals(ushort vehicleID, ref Vehicle vehicleData, ushort building)
        {
            if ((int)vehicleData.m_transferSize >= this.m_criminalCapacity)
            {
                return;
            }
            BuildingManager instance = Singleton<BuildingManager>.instance;
            CitizenManager instance2 = Singleton<CitizenManager>.instance;
            uint num = instance.m_buildings.m_buffer[(int)building].m_citizenUnits;
            int num2 = 0;
            while (num != 0u)
            {
                uint nextUnit = instance2.m_units.m_buffer[(int)((UIntPtr)num)].m_nextUnit;
                for (int i = 0; i < 5; i++)
                {
                    uint citizen = instance2.m_units.m_buffer[(int)((UIntPtr)num)].GetCitizen(i);
                    if (citizen != 0u && (instance2.m_citizens.m_buffer[(int)((UIntPtr)citizen)].Criminal || instance2.m_citizens.m_buffer[(int)((UIntPtr)citizen)].Arrested) && !instance2.m_citizens.m_buffer[(int)((UIntPtr)citizen)].Dead && instance2.m_citizens.m_buffer[(int)((UIntPtr)citizen)].GetBuildingByLocation() == building)
                    {
                        instance2.m_citizens.m_buffer[(int)((UIntPtr)citizen)].SetVehicle(citizen, vehicleID, 0u);
                        if (instance2.m_citizens.m_buffer[(int)((UIntPtr)citizen)].m_vehicle != vehicleID)
                        {
                            vehicleData.m_transferSize = (ushort)this.m_criminalCapacity;
                            return;
                        }
                        instance2.m_citizens.m_buffer[(int)((UIntPtr)citizen)].Arrested = true;
                        ushort instance3 = instance2.m_citizens.m_buffer[(int)((UIntPtr)citizen)].m_instance;
                        if (instance3 != 0)
                        {
                            instance2.ReleaseCitizenInstance(instance3);
                        }
                        instance2.m_citizens.m_buffer[(int)((UIntPtr)citizen)].CurrentLocation = Citizen.Location.Moving;
                        if ((int)(vehicleData.m_transferSize += 1) >= this.m_criminalCapacity)
                        {
                            return;
                        }
                    }
                }
                num = nextUnit;
                if (++num2 > 524288)
                {
                    CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
                    break;
                }
            }
        }

        private void CreatePolice(ushort vehicleID, ref Vehicle data, Citizen.AgePhase agePhase)
        {
            SimulationManager instance = Singleton<SimulationManager>.instance;
            CitizenManager instance2 = Singleton<CitizenManager>.instance;
            CitizenInfo groupCitizenInfo = instance2.GetGroupCitizenInfo(ref instance.m_randomizer, this.m_info.m_class.m_service, Citizen.Gender.Male, Citizen.SubCulture.Generic, agePhase);
            if (groupCitizenInfo != null)
            {
                int family = instance.m_randomizer.Int32(256u);
                uint num = 0u;
                if (instance2.CreateCitizen(out num, 90, family, ref instance.m_randomizer, groupCitizenInfo.m_gender))
                {
                    ushort num2;
                    if (instance2.CreateCitizenInstance(out num2, ref instance.m_randomizer, groupCitizenInfo, num))
                    {
                        Vector3 randomDoorPosition = data.GetRandomDoorPosition(ref instance.m_randomizer, VehicleInfo.DoorType.Exit);
                        groupCitizenInfo.m_citizenAI.SetCurrentVehicle(num2, ref instance2.m_instances.m_buffer[(int)num2], 0, 0u, randomDoorPosition);
                        groupCitizenInfo.m_citizenAI.SetTarget(num2, ref instance2.m_instances.m_buffer[(int)num2], data.m_targetBuilding);
                        instance2.m_citizens.m_buffer[(int)((UIntPtr)num)].SetVehicle(num, vehicleID, 0u);
                    }
                    else
                    {
                        instance2.ReleaseCitizen(num);
                    }
                }
            }
        }
    }
}
