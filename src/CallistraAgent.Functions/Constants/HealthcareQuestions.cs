namespace CallistraAgent.Functions.Constants;

/// <summary>
/// Healthcare questions for member outreach
/// </summary>
public static class HealthcareQuestions
{
    /// <summary>
    /// Question 1: Identity confirmation
    /// </summary>
    public const string Question1 = "Press 1 to confirm your identity. Press 2 if you cannot confirm.";

    /// <summary>
    /// Question 2: Program awareness
    /// </summary>
    public const string Question2 = "Press 1 if you are aware of your enrollment in your healthcare program. Press 2 if you are not aware.";

    /// <summary>
    /// Question 3: Assistance needs
    /// </summary>
    public const string Question3 = "Press 1 if you need assistance with your program. Press 2 if you do not need assistance.";

    /// <summary>
    /// Gets the question text by number (1-3)
    /// </summary>
    public static string GetQuestion(int questionNumber)
    {
        return questionNumber switch
        {
            1 => Question1,
            2 => Question2,
            3 => Question3,
            _ => throw new ArgumentOutOfRangeException(nameof(questionNumber), "Question number must be between 1 and 3")
        };
    }

    /// <summary>
    /// Total number of questions
    /// </summary>
    public const int TotalQuestions = 3;
}
