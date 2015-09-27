using ColossalFramework;
using ColossalFramework.DataBinding;
using ColossalFramework.Math;
using System;
using UnityEngine;
namespace Klyte.Unlimiter.Fake
{
public class FakeDepotAI : PlayerBuildingAI
{
	public TransportInfo m_transportInfo;
	public int m_noiseAccumulation = 100;
	public int m_maxVehicleCount = 100000;
		public float m_noiseRadius = 100f;


	protected override void ProduceGoods (ushort buildingID, ref Building buildingData, ref Building.Frame frameData, int productionRate, ref Citizen.BehaviourData behaviour, int aliveWorkerCount, int totalWorkerCount, int workPlaceCount, int aliveVisitorCount, int totalVisitorCount, int visitPlaceCount)
	{
		base.ProduceGoods (buildingID, ref buildingData, ref frameData, productionRate, ref behaviour, aliveWorkerCount, totalWorkerCount, workPlaceCount, aliveVisitorCount, totalVisitorCount, visitPlaceCount);
		int num = productionRate * this.m_noiseAccumulation / 100;
		if (num != 0)
		{
			Singleton<ImmaterialResourceManager>.instance.AddResource (ImmaterialResourceManager.Resource.NoisePollution, num, buildingData.m_position, this.m_noiseRadius);
		}
		base.HandleDead (buildingID, ref buildingData, ref behaviour, totalWorkerCount);
		TransferManager.TransferReason vehicleReason = this.m_transportInfo.m_vehicleReason;
		if (vehicleReason != TransferManager.TransferReason.None)
		{
			int num2 = (productionRate * this.m_maxVehicleCount + 99) / 100;
			if (this.m_transportInfo.m_transportType == TransportInfo.TransportType.Taxi)
			{
				DistrictManager instance = Singleton<DistrictManager>.instance;
				byte district = instance.GetDistrict (buildingData.m_position);
				District[] expr_B4_cp_0_cp_0 = instance.m_districts.m_buffer;
				byte expr_B4_cp_0_cp_1 = district;
				expr_B4_cp_0_cp_0 [(int)expr_B4_cp_0_cp_1].m_productionData.m_tempTaxiCapacity = expr_B4_cp_0_cp_0 [(int)expr_B4_cp_0_cp_1].m_productionData.m_tempTaxiCapacity + (uint)num2;
			}
			int num3 = 0;
			int num4 = 0;
			ushort num5 = 0;
			VehicleManager instance2 = Singleton<VehicleManager>.instance;
			ushort num6 = buildingData.m_ownVehicles;
			int num7 = 0;
			while (num6 != 0)
			{
				if ((TransferManager.TransferReason)instance2.m_vehicles.m_buffer [(int)num6].m_transferType == vehicleReason)
				{
					VehicleInfo info = instance2.m_vehicles.m_buffer [(int)num6].Info;
					int num8;
					int num9;
					info.m_vehicleAI.GetSize (num6, ref instance2.m_vehicles.m_buffer [(int)num6], out num8, out num9);
					num3++;
					if ((instance2.m_vehicles.m_buffer [(int)num6].m_flags & Vehicle.Flags.GoingBack) != Vehicle.Flags.None)
					{
						num4++;
					}
					else
					{
						if ((instance2.m_vehicles.m_buffer [(int)num6].m_flags & Vehicle.Flags.WaitingTarget) != Vehicle.Flags.None)
						{
							num5 = num6;
						}
						else
						{
							if (instance2.m_vehicles.m_buffer [(int)num6].m_targetBuilding != 0)
							{
								num5 = num6;
							}
						}
					}
				}
				num6 = instance2.m_vehicles.m_buffer [(int)num6].m_nextOwnVehicle;
				if (++num7 > 65536)
				{
					CODebugBase<LogChannel>.Error (LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
					break;
				}
			}
			if (this.m_maxVehicleCount < 65536 && num3 - num4 > num2 && num5 != 0)
			{
				VehicleInfo info2 = instance2.m_vehicles.m_buffer [(int)num5].Info;
				info2.m_vehicleAI.SetTarget (num5, ref instance2.m_vehicles.m_buffer [(int)num5], buildingID);
			}
			if (num3 < num2)
			{
				TransferManager.TransferOffer offer = default(TransferManager.TransferOffer);
				offer.Priority = 0;
				offer.Building = buildingID;
				offer.Position = buildingData.m_position;
				offer.Amount = Mathf.Min (2, num2 - num3);
				offer.Active = true;
				Singleton<TransferManager>.instance.AddOutgoingOffer (vehicleReason, offer);
			}
		}
	}
}
}
