using System.ComponentModel;

namespace CogentAgent.Web.Models;

public class CogentGame(string creator)
{
    public string Title { get; set; }
    public string Setting { get; set; }
    public List<Character> Players { get; set; } = new();
    public List<Scene> Scenes { get; set; } = new();
    public int CurrentSceneIndex { get; set; } = 0;
    public GameStatus Status { get; set; } = GameStatus.NotStarted;

    public Scene? CurrentScene => Scenes.ElementAtOrDefault(CurrentSceneIndex);

    [Description("🚀 Initialize a new game session")]
    public string InitializeGame(string title, string setting)
    {
        Title = title;
        Setting = setting;
        Status = GameStatus.InProgress;
        CurrentSceneIndex = 0;
        Players.Clear();
        Scenes.Clear();
        return $"Game '{title}' initialized in setting '{setting}'.";
    }

    [Description("🧑‍🎤 Add a new character to the game")]
    public string AddCharacter(string name, string concept, int health = 10, int resolve = 5)
    {
        if (Players.Any(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            return $"Character '{name}' already exists.";

        var character = new Character
        {
            Name = name,
            Concept = concept,
            Health = health,
            Resolve = resolve,
            Attributes = new List<Attribute>
            {
                new() { Name = "Dexterity", Value = 3 },
                new() { Name = "Intelligence", Value = 3 }
            },
            Skills = new List<Skill>
            {
                new() { Name = "Stealth", Rank = 2 },
                new() { Name = "Persuasion", Rank = 2 }
            }
        };

        Players.Add(character);
        return $"Character '{name}' added with concept '{concept}'.";
    }

    [Description("🎲 Roll dice for a character using attribute + skill")]
    public DiceRoll RollDice(string characterName, string skillName)
    {
        var character = Players.FirstOrDefault(c => c.Name.Equals(characterName, StringComparison.OrdinalIgnoreCase));
        if (character == null) throw new ArgumentException("Character not found.");

        var attr = character.Attributes.FirstOrDefault()?.Value ?? 0;
        var skill = character.Skills.FirstOrDefault(s => s.Name == skillName)?.Rank ?? 0;

        var totalDice = attr + skill;
        var rolls = new List<int>();
        var successes = 0;
        var rng = new Random();

        for (int i = 0; i < totalDice; i++)
        {
            var roll = rng.Next(1, 7);
            rolls.Add(roll);
            if (roll >= 4) successes++;
        }

        return new DiceRoll
        {
            DiceCount = totalDice,
            Rolls = rolls,
            Successes = successes
        };
    }

    [Description("🧠 Apply a skill check and reduce resolve if failed")] 
    public string ApplySkillCheck(string characterName, string skillName, int difficulty)
    {
        var character = Players.FirstOrDefault(c => c.Name.Equals(characterName, StringComparison.OrdinalIgnoreCase));
        if (character == null) return $"Character '{characterName}' not found.";

        var result = RollDice(characterName, skillName);
        var penalty = Math.Max(0, difficulty - result.Successes);
        character.Resolve -= penalty;

        return $"{characterName} rolled {result.Successes} successes. Resolve reduced by {penalty}.";
    }

    [Description("🎭 Advance to the next scene")]
    public string AdvanceScene()
    {
        if (CurrentSceneIndex + 1 < Scenes.Count)
        {
            CurrentSceneIndex++;
            return $"Scene advanced to: {CurrentScene?.Title}";
        }
        else
        {
            Status = GameStatus.Completed;
            return "The game has concluded.";
        }
    }

    [Description("📖 Narrate the current scene")] 
    public string NarrateScene()
    {
        var scene = CurrentScene;
        return scene == null
            ? "No scene is currently active."
            : $"Scene: {scene.Title}\n{scene.Description}";
    }

    [Description("🎤 Respond to a player action")] 
    public string RespondToAction(string characterName, string action)
    {
        var character = Players.FirstOrDefault(c => c.Name.Equals(characterName, StringComparison.OrdinalIgnoreCase));
        if (character == null) return $"Character '{characterName}' not found.";

        return $"{character.Name} attempts to {action}. The outcome depends on their skills and the scene context.";
    }
}
