using ColossalFramework;
using ColossalFramework.Math;
using System;
using UnityEngine;

namespace Klyte.Unlimiter.Fake
{
public class FakeVehicle 
{

		public ushort m_leadingVehicle;
		
		public ushort m_transportLine;
		
		public ushort m_cargoParent;
		
		public ushort m_trailingVehicle;
		
		public ushort m_nextGuestVehicle;
		
		public ushort m_nextOwnVehicle;
		
		public ushort m_transferSize;
		
		public ushort m_nextLineVehicle;
		
		public ushort m_firstCargo;
		
		public byte m_pathPositionIndex;
		
		public byte m_blockCounter;
		
		public byte m_gateIndex;
		
		public byte m_lastPathOffset;
		
		public ushort m_infoIndex;
		
		public ushort m_nextCargo;
		
		public byte m_waitCounter;
		
		public byte m_transferType;
		
		public Segment3 m_segment;
		
		public Vehicle.Frame m_frame3;
		
		public Vector4 m_targetPos1;
		
		public Vector4 m_targetPos0;
		
		public Vehicle.Frame m_frame0;
		
		public byte m_lastFrame;
		
		public Vehicle.Frame m_frame2;
		
		public Vehicle.Frame m_frame1;
		
		public ushort m_sourceBuilding;
		
		public uint m_path;
		
		public ushort m_nextGridVehicle;
		
		public ushort m_targetBuilding;
		
		public Vector4 m_targetPos2;
		
		public Vector4 m_targetPos3;
		
		public uint m_citizenUnits;
		
		public Vehicle.Flags m_flags;


	

		public static bool GetClosestFreeTrailer (ushort vehicleID, Vector3 position, out ushort trailerID, out uint unitID)
		{
			VehicleManager instance = Singleton<VehicleManager>.instance;
			float num = 1E+10f;
			trailerID = 0;
			unitID = 0u;
			int num2 = 0;
			while (vehicleID != 0)
			{
				float num3 = Vector3.SqrMagnitude (position - instance.m_vehicles.m_buffer [(int)vehicleID].GetLastFrameData ().m_position);
				if (num3 < num)
				{
					uint notFullCitizenUnit = instance.m_vehicles.m_buffer [(int)vehicleID].GetNotFullCitizenUnit (CitizenUnit.Flags.Vehicle);
					if (notFullCitizenUnit != 0u)
					{
						num = num3;
						trailerID = vehicleID;
						unitID = notFullCitizenUnit;
					}
				}
				vehicleID = instance.m_vehicles.m_buffer [(int)vehicleID].m_trailingVehicle;
				if (++num2 > 65536)
				{
					CODebugBase<LogChannel>.Error (LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
					break;
				}
			}
			return trailerID != 0;
		}

		public float CalculateTotalLength (ushort vehicleID)
		{


					VehicleInfo info =  PrefabCollection<VehicleInfo>.GetPrefab ((uint)this.m_infoIndex);
				
			float num = info.m_generatedInfo.m_size.z;
			if ((this.m_flags & Vehicle.Flags.Spawned) == Vehicle.Flags.None)
			{
				if (info.m_trailers != null)
				{
					float num2 = ((this.m_flags & Vehicle.Flags.Inverted) == Vehicle.Flags.None) ? info.m_attachOffsetBack : info.m_attachOffsetFront;
					Randomizer randomizer = new Randomizer ((int)vehicleID);
					for (int i = 0; i < info.m_trailers.Length; i++)
					{
						if (randomizer.Int32 (100u) < info.m_trailers [i].m_probability)
						{
							num -= num2;
							VehicleInfo info2 = info.m_trailers [i].m_info;
							bool flag = randomizer.Int32 (100u) < info.m_trailers [i].m_invertProbability;
							num += info2.m_generatedInfo.m_size.z;
							num -= ((!flag) ? info2.m_attachOffsetFront : info2.m_attachOffsetBack);
							num2 = ((!flag) ? info2.m_attachOffsetBack : info2.m_attachOffsetFront);
						}
					}
				}
			}
			else
			{
				if (this.m_leadingVehicle == 0 && this.m_trailingVehicle != 0)
				{
					num -= (((this.m_flags & Vehicle.Flags.Inverted) == Vehicle.Flags.None) ? info.m_attachOffsetBack : info.m_attachOffsetFront);
					VehicleManager instance = Singleton<VehicleManager>.instance;
					ushort num3 = this.m_trailingVehicle;
					int num4 = 0;
					while (num3 != 0)
					{
						ushort trailingVehicle = instance.m_vehicles.m_buffer [(int)num3].m_trailingVehicle;
						VehicleInfo info3 = instance.m_vehicles.m_buffer [(int)num3].Info;
						num += info3.m_generatedInfo.m_size.z;
						if ((instance.m_vehicles.m_buffer [(int)num3].m_flags & Vehicle.Flags.Inverted) != Vehicle.Flags.None)
						{
							num -= info3.m_attachOffsetBack;
							if (trailingVehicle != 0)
							{
								num -= info3.m_attachOffsetFront;
							}
						}
						else
						{
							num -= info3.m_attachOffsetFront;
							if (trailingVehicle != 0)
							{
								num -= info3.m_attachOffsetBack;
							}
						}
						num3 = trailingVehicle;
						if (++num4 > 65536)
						{
							CODebugBase<LogChannel>.Error (LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
							break;
						}
					}
				}
			}
			return num;
		}
		public ushort GetFirstVehicle (ushort vehicleID)
		{
			if (this.m_leadingVehicle == 0)
			{
				return vehicleID;
			}
			VehicleManager instance = Singleton<VehicleManager>.instance;
			ushort leadingVehicle = this.m_leadingVehicle;
			int num = 0;
			while (leadingVehicle != 0)
			{
				vehicleID = leadingVehicle;
				leadingVehicle = instance.m_vehicles.m_buffer [(int)vehicleID].m_leadingVehicle;
				if (++num > 65536)
				{
					CODebugBase<LogChannel>.Error (LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
					break;
				}
			}
			return vehicleID;
		}
		public ushort GetLastVehicle (ushort vehicleID)
		{
			if (this.m_trailingVehicle == 0)
			{
				return vehicleID;
			}
			VehicleManager instance = Singleton<VehicleManager>.instance;
			ushort trailingVehicle = this.m_trailingVehicle;
			int num = 0;
			while (trailingVehicle != 0)
			{
				vehicleID = trailingVehicle;
				trailingVehicle = instance.m_vehicles.m_buffer [(int)vehicleID].m_trailingVehicle;
				if (++num > 65536)
				{
					CODebugBase<LogChannel>.Error (LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
					break;
				}
			}
			return vehicleID;
		}
		private uint GetTargetFrame (VehicleInfo info, ushort vehicleID)
		{
			if (info.m_vehicleType != VehicleInfo.VehicleType.Bicycle)
			{
				ushort firstVehicle = this.GetFirstVehicle (vehicleID);
				uint num = (uint)(((int)firstVehicle << 4) / 16384);
				return Singleton<SimulationManager>.instance.m_referenceFrameIndex - num;
			}
			CitizenManager instance = Singleton<CitizenManager>.instance;
			ushort num2 = 0;
			if (this.m_citizenUnits != 0u)
			{
				uint citizen = instance.m_units.m_buffer [(int)((UIntPtr)this.m_citizenUnits)].m_citizen0;
				if (citizen != 0u)
				{
					num2 = instance.m_citizens.m_buffer [(int)((UIntPtr)citizen)].m_instance;
				}
			}
			if (num2 != 0)
			{
				uint num3 = (uint)(((int)num2 << 4) / 65536);
				return Singleton<SimulationManager>.instance.m_referenceFrameIndex - num3;
			}
			uint num4 = (uint)(((int)vehicleID << 4) / 16384);
			return Singleton<SimulationManager>.instance.m_referenceFrameIndex - num4;
		}
		public void Unspawn (ushort vehicleID)
		{
			VehicleManager instance = Singleton<VehicleManager>.instance;
			if (this.m_leadingVehicle == 0 && this.m_trailingVehicle != 0)
			{
				ushort num = this.m_trailingVehicle;
				this.m_trailingVehicle = 0;
				int num2 = 0;
				while (num != 0)
				{
					ushort trailingVehicle = instance.m_vehicles.m_buffer [(int)num].m_trailingVehicle;
					instance.m_vehicles.m_buffer [(int)num].m_leadingVehicle = 0;
					instance.m_vehicles.m_buffer [(int)num].m_trailingVehicle = 0;
					instance.ReleaseVehicle (num);
					num = trailingVehicle;
					if (++num2 > 65536)
					{
						CODebugBase<LogChannel>.Error (LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
						break;
					}
				}
			}
			if ((this.m_flags & Vehicle.Flags.Spawned) != Vehicle.Flags.None)
			{
				VehicleInfo info =  PrefabCollection<VehicleInfo>.GetPrefab ((uint)this.m_infoIndex);
				if (info != null)
				{
					object thisVeh = this;
					Vehicle thisVehAsVeh = ((Vehicle) thisVeh);
					instance.RemoveFromGrid (vehicleID, ref thisVehAsVeh, info.m_isLargeVehicle);

				}
				this.m_flags &= ~Vehicle.Flags.Spawned;
			}
		}



}
}

