using UnityEngine;

namespace OdinPlus
{
	class SE_SumonPet : StatusEffect
	{
		public string PetName;
		private GameObject _spawnedPet;

		public override void Setup(Character character)
		{
			base.Setup(character);
			_spawnedPet = PetManager.SummonPet(PetName);
		}

		public override void UpdateStatusEffect(float dt)
		{
			base.UpdateStatusEffect(dt);
		}

		public override void Stop()
		{
			base.Stop();
			// Despawn pet when status effect expires
			if (_spawnedPet != null)
			{
				DBG.blogInfo($"[SE_SummonPet] Status effect expired, despawning {PetName}");
				ZNetScene.instance.Destroy(_spawnedPet);
				_spawnedPet = null;
			}
		}
	}
}
