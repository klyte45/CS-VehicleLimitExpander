using ColossalFramework;
using ColossalFramework.Math;
using System;
using System.Threading;
using UnityEngine;

namespace Klyte.Unlimiter.Fake
{
	public class FakeTransportLine
	{
		public ushort m_vehicles;
		public TransportPassengerData m_passengers;
		public TransportInfo Info;
		public bool Complete;
		public TransportLine.Flags m_flags;
		public ushort m_stops;

		public int CountVehicles (ushort lineID)
		{
			VehicleManager instance = Singleton<VehicleManager>.instance;
			ushort num = this.m_vehicles;
			int num2 = 0;
			int num3 = 0;
			while (num != 0) {
				num2++;
				num = instance.m_vehicles.m_buffer [(int)num].m_nextLineVehicle;
				if (++num3 >= 65536) {
					CODebugBase<LogChannel>.Error (LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
					break;
				}
			}
			return num2;
		}

		public void SetVehicleTargetStop (ushort lineID, ushort oldStop, ushort newStop)
		{
			if (this.m_vehicles != 0) {
				VehicleManager instance = Singleton<VehicleManager>.instance;
				ushort num = this.m_vehicles;
				int num2 = 0;
				while (num != 0) {
					if (oldStop == 0 || oldStop == instance.m_vehicles.m_buffer [(int)num].m_targetBuilding) {
						VehicleInfo info = instance.m_vehicles.m_buffer [(int)num].Info;
						info.m_vehicleAI.SetTarget (num, ref instance.m_vehicles.m_buffer [(int)num], newStop);
					}
					num = instance.m_vehicles.m_buffer [(int)num].m_nextLineVehicle;
					if (++num2 > 65536) {
						CODebugBase<LogChannel>.Error (LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
						break;
					}
				}
			}
		}

		public void RemoveVehicle (ushort vehicleID, ref Vehicle data)
		{
			VehicleManager instance = Singleton<VehicleManager>.instance;
			ushort num = 0;
			ushort num2 = this.m_vehicles;
			int num3 = 0;
			while (num2 != 0) {
				if (num2 == vehicleID) {
					if (num != 0) {
						instance.m_vehicles.m_buffer [(int)num].m_nextLineVehicle = data.m_nextLineVehicle;
					} else {
						this.m_vehicles = data.m_nextLineVehicle;
					}
					data.m_nextLineVehicle = 0;
					data.m_targetBuilding = 0;
					return;
				}
				num = num2;
				num2 = instance.m_vehicles.m_buffer [(int)num2].m_nextLineVehicle;
				if (++num3 > 65536) {
					CODebugBase<LogChannel>.Error (LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
					break;
				}
			}
		}

		public ushort GetVehicle (int index)
		{
			VehicleManager instance = Singleton<VehicleManager>.instance;
			ushort num = this.m_vehicles;
			int num2 = 0;
			while (num != 0) {
				if (index-- == 0) {
					return num;
				}
				num = instance.m_vehicles.m_buffer [(int)num].m_nextLineVehicle;
				if (++num2 >= 65536) {
					CODebugBase<LogChannel>.Error (LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
					break;
				}
			}
			return 0;
		}
		
		public ushort GetStop (int index)
		{
			ushort stops = this.m_stops;
			ushort num = stops;
			int num2 = 0;
			while (num != 0) {
				if (index-- == 0) {
					return num;
				}
				num = TransportLine.GetNextStop (num);
				if (num == stops) {
					break;
				}
				if (++num2 >= 32768) {
					CODebugBase<LogChannel>.Error (LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
					break;
				}
			}
			return 0;
		}

		public void SimulationStep (ushort lineID)
		{
			TransportInfo info = this.Info;
			if (this.Complete) {
				int num = 0;
				if (this.m_vehicles != 0) {
					VehicleManager instance = Singleton<VehicleManager>.instance;
					ushort num2 = this.m_vehicles;
					int num3 = 0;
					while (num2 != 0) {
						ushort nextLineVehicle = instance.m_vehicles.m_buffer [(int)num2].m_nextLineVehicle;
						num++;
						num2 = nextLineVehicle;
						if (++num3 > 65536) {
							CODebugBase<LogChannel>.Error (LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
							break;
						}
					}
				}
				bool flag;
				if (Singleton<SimulationManager>.instance.m_isNightTime) {
					flag = ((this.m_flags & TransportLine.Flags.DisabledNight) == TransportLine.Flags.None);
				} else {
					flag = ((this.m_flags & TransportLine.Flags.DisabledDay) == TransportLine.Flags.None);
				}
				uint num4 = 0u;
				float num5 = 0f;
				if (this.m_stops != 0) {
					NetManager instance2 = Singleton<NetManager>.instance;
					ushort stops = this.m_stops;
					ushort num6 = stops;
					int num7 = 0;
					while (num6 != 0) {
						ushort num8 = 0;
						if (flag) {
							NetNode[] expr_107_cp_0 = instance2.m_nodes.m_buffer;
							ushort expr_107_cp_1 = num6;
							expr_107_cp_0 [(int)expr_107_cp_1].m_flags = (expr_107_cp_0 [(int)expr_107_cp_1].m_flags & ~NetNode.Flags.Disabled);
						} else {
							NetNode[] expr_12D_cp_0 = instance2.m_nodes.m_buffer;
							ushort expr_12D_cp_1 = num6;
							expr_12D_cp_0 [(int)expr_12D_cp_1].m_flags = (expr_12D_cp_0 [(int)expr_12D_cp_1].m_flags | NetNode.Flags.Disabled);
						}
						for (int i = 0; i < 8; i++) {
							ushort segment = instance2.m_nodes.m_buffer [(int)num6].GetSegment (i);
							if (segment != 0 && instance2.m_segments.m_buffer [(int)segment].m_startNode == num6) {
								num5 += instance2.m_segments.m_buffer [(int)segment].m_averageLength;
								num8 = instance2.m_segments.m_buffer [(int)segment].m_endNode;
								break;
							}
						}
						num4 += 1u;
						num6 = num8;
						if (num6 == stops) {
							break;
						}
						if (++num7 >= 32768) {
							CODebugBase<LogChannel>.Error (LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
							break;
						}
					}
				}
				int num9 = num * info.m_maintenanceCostPerVehicle / 100;
				if (num9 != 0) {
					Singleton<EconomyManager>.instance.FetchResource (EconomyManager.Resource.Maintenance, num9, info.m_class);
				}
				int budget = Singleton<EconomyManager>.instance.GetBudget (info.m_class);
				int num10;
				if (flag) {
					num10 = Mathf.CeilToInt ((float)budget * num5 / (info.m_defaultVehicleDistance * 100f));
				} else {
					num10 = 0;
				}
				if (num4 != 0u && num < num10) {
					TransferManager.TransferReason vehicleReason = info.m_vehicleReason;
					int index = Singleton<SimulationManager>.instance.m_randomizer.Int32 (num4);
					ushort stop = this.GetStop (index);
					if (vehicleReason != TransferManager.TransferReason.None && stop != 0) {
						TransferManager.TransferOffer offer = default(TransferManager.TransferOffer);
						offer.Priority = num10 - num + 1;
						offer.TransportLine = lineID;
						offer.Position = Singleton<NetManager>.instance.m_nodes.m_buffer [(int)stop].m_position;
						offer.Amount = 1;
						offer.Active = false;
						Singleton<TransferManager>.instance.AddIncomingOffer (vehicleReason, offer);
					}
				} else {
					if (num > num10) {
						int index2 = Singleton<SimulationManager>.instance.m_randomizer.Int32 ((uint)num);
						ushort vehicle = this.GetVehicle (index2);
						if (vehicle != 0) {
							VehicleManager instance3 = Singleton<VehicleManager>.instance;
							VehicleInfo info2 = instance3.m_vehicles.m_buffer [(int)vehicle].Info;
							info2.m_vehicleAI.SetTransportLine (vehicle, ref instance3.m_vehicles.m_buffer [(int)vehicle], 0);
						}
					}
				}
			}
			if ((Singleton<SimulationManager>.instance.m_currentFrameIndex & 4095u) >= 3840u) {
				this.m_passengers.Update ();
				Singleton<TransportManager>.instance.m_passengers [(int)info.m_transportType].Add (ref this.m_passengers);
				this.m_passengers.Reset ();
			}
		}

	}

}