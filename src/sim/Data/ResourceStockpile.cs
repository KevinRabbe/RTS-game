namespace RtsGame.Sim.Data
{
    public sealed class ResourceStockpile
    {
        public int Food { get; set; }
        public int Wood { get; set; }
        public int Gold { get; set; }

        public int Get(ResourceType resourceType)
        {
            switch (resourceType)
            {
                case ResourceType.Food:
                    return Food;
                case ResourceType.Wood:
                    return Wood;
                case ResourceType.Gold:
                    return Gold;
                default:
                    return 0;
            }
        }

        public void Add(ResourceType resourceType, int amount)
        {
            switch (resourceType)
            {
                case ResourceType.Food:
                    Food += amount;
                    break;
                case ResourceType.Wood:
                    Wood += amount;
                    break;
                case ResourceType.Gold:
                    Gold += amount;
                    break;
            }
        }

        public bool CanPay(ResourceStockpile cost)
        {
            return Food >= cost.Food && Wood >= cost.Wood && Gold >= cost.Gold;
        }

        public void Subtract(ResourceStockpile cost)
        {
            Food -= cost.Food;
            Wood -= cost.Wood;
            Gold -= cost.Gold;
        }
    }
}
