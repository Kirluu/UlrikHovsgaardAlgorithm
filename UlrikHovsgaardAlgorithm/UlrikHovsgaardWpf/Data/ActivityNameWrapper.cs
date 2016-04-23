namespace UlrikHovsgaardWpf.Data
{
    public class ActivityNameWrapper
    {
        //public ICommand Command { get; set; }
        public string DisplayName { get; set; }

        public ActivityNameWrapper(string name)
        {
            //Command = cmd;
            DisplayName = name;
        }
    }
}
