using UnityEngine;

namespace OdinPlus
{
	public class GuardNPC : HumanNPC, Hoverable
	{
		static readonly string[] GuardGreetings = {
			"$op_guard_greet1", "$op_guard_greet2", "$op_guard_greet3",
			"$op_guard_greet4", "$op_guard_greet5"
		};

		protected override void Awake()
		{
			base.Awake();
			m_hum.m_onDamaged = (System.Action<float, Character>)System.Delegate.Combine(
				m_hum.m_onDamaged, new System.Action<float, Character>(OnDamaged));
		}

		void OnDamaged(float hit, Character attacker)
		{
			if(attacker == null || !attacker.IsPlayer()) return;
			var playerID = Player.m_localPlayer.GetZDOID().ToString();
			if(ZNet.instance.IsServer())
				FactionManager.ModifyReputation(playerID, FactionName, FactionManager.EventValues.NPCDamaged);
			else
				ZRoutedRpc.instance.InvokeRoutedRPC(0L, "ReputationChange", playerID, FactionName, FactionManager.EventValues.NPCDamaged);
		}

		public override bool Interact(Humanoid user, bool hold, bool alt)
		{
			if(hold) return false;
			if(m_hum.m_faction != Character.Faction.Players) return false;
			Say(GuardGreetings[Random.Range(0, GuardGreetings.Length)]);
			return true;
		}

		public override string GetHoverText()
		{
			if(m_hum.m_faction != Character.Faction.Players) return "";
			var text = $"<color=lightblue><b>{m_name}</b></color>\n<color=grey>({FactionName} Guard)</color>";
			text += "\n[<color=yellow><b>$KEY_Use</b></color>] $op_talk";
			return Localization.instance.Localize(text);
		}

		public override string GetHoverName() => Localization.instance.Localize(m_name);

		public override void SecondaryInteract(Humanoid user) { }
		public override bool UseItem(Humanoid user, ItemDrop.ItemData item) => false;
	}
}
