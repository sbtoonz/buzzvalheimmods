using UnityEngine;

namespace OdinPlus
{
	public class GuardNPC : HumanNPC, Hoverable
	{
		protected override void Awake()
		{
			base.Awake();
			var zdo = m_nview.GetZDO();
			m_hum.m_onDamaged = (System.Action<float, Character>)System.Delegate.Combine(
				m_hum.m_onDamaged, new System.Action<float, Character>(OnDamaged));
		}

		private void OnDamaged(float hit, Character attacker)
		{
			if (attacker == null || !attacker.IsPlayer()) return;
			string playerID = Player.m_localPlayer.GetZDOID().ToString();
			if (ZNet.instance.IsServer())
				FactionManager.ModifyReputation(playerID, FactionName, FactionManager.EventValues.NPCDamaged);
			else
				ZRoutedRpc.instance.InvokeRoutedRPC(0L, "ReputationChange", playerID, FactionName, FactionManager.EventValues.NPCDamaged);
		}

		public override string GetHoverText()
		{
			if (m_hum.m_faction != Character.Faction.Players) return "";
			return Localization.instance.Localize(string.Format("<color=lightblue><b>{0}</b></color>\n<color=grey>({1})</color>", m_name, FactionName));
		}

		public override string GetHoverName()
		{
			return Localization.instance.Localize(m_name);
		}
	}
}
