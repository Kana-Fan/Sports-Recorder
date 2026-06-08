namespace SportsRecorder.Model
{
    internal class StrengthItem : BaseModel
    {
        private string _bodyPart = "";
        public string BodyPart
        {
            get => _bodyPart;
            set
            {
                _bodyPart = value;
                OnPropertyChanged(nameof(BodyPart));
            }
        }

        private string _name = "";
        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                OnPropertyChanged(nameof(Name));
            }
        }
    }
}
