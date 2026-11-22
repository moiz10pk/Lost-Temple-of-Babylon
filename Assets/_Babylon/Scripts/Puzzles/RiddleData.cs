namespace Babylon
{
    /// <summary>The riddle pool. Each has four options; 'answer' is the index of the right one.</summary>
    public static class RiddleData
    {
        public struct Riddle
        {
            public string text; public string[] options; public int answer;
            public Riddle(string t, int a, params string[] o) { text = t; answer = a; options = o; }
        }

        public static readonly Riddle[] Pool =
        {
            new Riddle("I have cities but no houses, rivers but no water, mountains but no stone. What am I?", 2, "A dream", "A desert", "A map", "A painting"),
            new Riddle("The more you take, the more you leave behind. What am I?", 0, "Footsteps", "Breath", "Gold", "Time"),
            new Riddle("I am pressed into wet clay with a reed; the oldest way men kept their words. What am I?", 3, "A seal", "A brick", "Pottery", "Cuneiform writing"),
            new Riddle("What has to be broken before you can use it?", 1, "A promise", "An egg", "A lock", "A silence"),
            new Riddle("I rise in the east and set in the west; Babylon called me Shamash. What am I?", 2, "The moon", "The wind", "The sun", "The river"),
            new Riddle("Two rivers embrace the land between them; the Greeks named it for that. Which rivers?", 0, "Tigris and Euphrates", "Nile and Jordan", "Indus and Ganges", "Danube and Rhine"),
            new Riddle("I speak without a mouth and hear without ears. I have no body, but I come alive with the wind. What am I?", 3, "A ghost", "A flute", "A shadow", "An echo"),
            new Riddle("The king who carved his laws in black stone for all of Babylon to read was...", 1, "Nebuchadnezzar", "Hammurabi", "Gilgamesh", "Sargon"),
        };
    }
}
