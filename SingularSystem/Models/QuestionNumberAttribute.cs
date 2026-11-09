namespace SingularSystem.Models
{
    [AttributeUsage(AttributeTargets.Property)]
    public class QuestionNumberAttribute : Attribute
    {
        public int Number { get; }
        public QuestionNumberAttribute(int number)
        {
            Number = number;
        }
    }
}
