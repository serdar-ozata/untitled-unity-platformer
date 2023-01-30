namespace Functionality.Controllable {
    public interface IControllable {
        public void Initialize();
        /**
         * <summary>
         * Returns false if object can't perform Execute() e.g. there is an obstacle on the path (for instance: player)
         * </summary>
         */
        public bool IsValid();
        public void Execute();
    }
}