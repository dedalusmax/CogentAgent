using System.Diagnostics;

namespace CogentAgent.Web.Models;

public class Character
{
    public string Name { get; set; }
    public string Concept { get; set; } // e.g., "Stealthy rogue", "Space pilot"
    public int Level { get; set; }
    public List<Attribute> Attributes { get; set; } = new();
    public List<Skill> Skills { get; set; } = new();
    public List<Trait> Traits { get; set; } = new();
    public List<Item> Inventory { get; set; } = new();
    public int Health { get; set; }
    public int Resolve { get; set; }
}
