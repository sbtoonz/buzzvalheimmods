using System;

namespace OdinPlus
{
	public class HumanWorker : HumanVillager, Hoverable, Interactable, OdinInteractable
	{
		protected override void Awake()
		{
			base.Awake();
			m_name = "Worker";
			ChoiceList = new string[1] { "$op_talk" };
		}
		public override void Choice0()
		{
			string npcName = m_nview.GetZDO().GetString("npcname", "");
			if (!string.IsNullOrEmpty(npcName) && OdinData.GetKey(npcName))
			{
				OdinData.RemoveKey(npcName);
				HumanMessager.RemoveQuestPin(npcName);
				OdinData.AddCredits(10, true);
				Say("Thanks for the delivery!");
				return;
			}
			Say("I'm just working here.");
		}
	}
}
