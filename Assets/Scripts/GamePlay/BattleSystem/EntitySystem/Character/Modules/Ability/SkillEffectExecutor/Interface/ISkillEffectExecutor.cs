namespace GamePlay.BattleSystem
{
    public interface ISkillEffectExecutor
    {
        void ExecuteSkillEffect(SkillEffect effect);
        void StopExecution();
        
        void Tick(float deltaTime);
    }
}