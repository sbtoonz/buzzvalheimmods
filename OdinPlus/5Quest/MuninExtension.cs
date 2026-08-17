using System;

namespace OdinPlus
{
	public static class MuninExtension
	{
		public static void SetMuninHints(this Quest quest)
		{
			switch(quest.GetQuestType())
			{
				case QuestType.Hunt:
					quest.HintStart = $"$op_quest_hunt_start_pr_1 <color=yellow><b>[{quest.locName}]</b></color> $op_quest_hunt_start_po_1 ";
					quest.HintTarget = $"$op_quest_hunt_target_pr_1 <color=yellow><b>[{quest.locName}]</b></color> $op_quest_hunt_target_po_1 ";
					break;
				case QuestType.Treasure:
					quest.HintStart = $"$op_quest_treasure_start_pr_1 <color=yellow><b>[{quest.locName}]</b></color> $op_quest_treasure_start_po_1 ";
					quest.HintTarget = $"$op_quest_treasure_target_pr_1 <color=yellow><b>[{quest.locName}]</b></color> $op_quest_treasure_target_po_1 ";
					break;
				case QuestType.Dungeon:
					quest.HintStart = $"$op_quest_dungeon_start_pr_1 <color=yellow><b>[{quest.locName}]</b></color> $op_quest_dungeon_start_po_1 ";
					quest.HintTarget = $"$op_quest_dungeon_target_pr_1 <color=yellow><b>[{quest.locName}]</b></color> $op_quest_dungeon_target_po_1 ";
					break;
				case QuestType.Search:
					quest.HintStart = $"$op_quest_search_start_pr_1 <color=yellow><b>[{quest.locName}]</b></color> $op_quest_search_start_po_1 ";
					break;
			}
		}
		public static void SetMuninMessage(this Quest quest) =>
			quest.m_message = $"{quest.m_index} $op_quest_side  $op_quest_quest \n{quest.QuestName}";

	}
}