namespace BehaviorSimulation.Core
{
    public interface ISimulation
    {
        void Play();
        void Pause();
        void Step();
        void ResetSimulation();
    }
}
