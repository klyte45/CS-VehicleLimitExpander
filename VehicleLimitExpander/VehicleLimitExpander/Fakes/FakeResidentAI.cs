using ColossalFramework;
using ColossalFramework.Globalization;
using ColossalFramework.Math;
using ColossalFramework.Steamworks;
using ColossalFramework.Threading;
using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Klyte.Unlimiter.Fake
{
	public class FakeResidentAI
	{

		private bool DoRandomMove ()
		{
			uint vehicleCount = (uint)Singleton<VehicleManager>.instance.m_vehicleCount;
			uint instanceCount = (uint)Singleton<CitizenManager>.instance.m_instanceCount;
			if (vehicleCount * 65536u > instanceCount * 65536u) {
				return Singleton<SimulationManager>.instance.m_randomizer.UInt32 (65536u) > vehicleCount;
			}
			return Singleton<SimulationManager>.instance.m_randomizer.UInt32 (65536u) > instanceCount;
		}
	}

}