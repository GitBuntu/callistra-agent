namespace CallistraAgent.Functions.Constants;

/// <summary>
/// Healthcare questions for member outreach
/// </summary>
public static class HealthcareQuestions
{
    /// <summary>
    /// Question 1: Identity confirmation
    /// </summary>
    public const string Question1 = "This is a call about your healthcare enrollment. Can you confirm you are available to answer a few questions? Press 1 for yes, 2 for no.";

    /// <summary>
    /// Question 2: Program awareness
    /// </summary>
    public const string Question2 = "Are you aware you are enrolled in a healthcare program? Press 1 for yes, 2 for no.";

    /// <summary>
    /// Question 3: Assistance needs
    /// </summary>
    public const string Question3 = "Would you like assistance with your healthcare services? Press 1 for yes, 2 for no.";

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
