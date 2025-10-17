namespace CogentAgent.Web.Models;

public class Item
{
    public string Name { get; set; }
    public string Type { get; set; } // e.g., Weapon, Tool, Consumable
    public string Description { get; set; }
    public bool IsEquipped { get; set; }
}
