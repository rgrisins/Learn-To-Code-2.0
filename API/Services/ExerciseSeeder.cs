using LearnToCode.Data;
using Microsoft.EntityFrameworkCore;

namespace LearnToCode.API.Services;

internal static class ExerciseSeeder
{
    private const int MinimumTestCaseCount = 20;

    public static async Task SeedAsync(AppDbContext dbContext)
    {
        var exercises = await dbContext.Exercises
            .Include(exercise => exercise.TestCases)
            .ToListAsync();

        var now = DateTime.UtcNow;

        foreach (var seed in GetSeeds())
        {
            if (seed.TestCases.Count < MinimumTestCaseCount)
            {
                throw new InvalidOperationException($"Exercise seed '{seed.Title}' must have at least {MinimumTestCaseCount} tests.");
            }

            var exercise = exercises.FirstOrDefault(item => item.Title == seed.Title);
            if (exercise is null)
            {
                exercise = new Exercise
                {
                    Title = seed.Title,
                    Description = seed.Description,
                    LanguageCode = seed.LanguageCode,
                    LanguageVersion = seed.LanguageVersion,
                    Difficulty = seed.Difficulty,
                    SortOrder = seed.SortOrder,
                    CreatedAtUtc = now,
                };

                dbContext.Exercises.Add(exercise);
                exercises.Add(exercise);
            }
            else
            {
                exercise.Description = seed.Description;
                exercise.LanguageCode = seed.LanguageCode;
                exercise.LanguageVersion = seed.LanguageVersion;
                exercise.Difficulty = seed.Difficulty;
                exercise.SortOrder = seed.SortOrder;
            }

            SyncTestCases(exercise, seed);
        }

        await dbContext.SaveChangesAsync();
    }

    private static void SyncTestCases(Exercise exercise, ExerciseSeed seed)
    {
        foreach (var testCaseSeed in seed.TestCases.OrderBy(testCase => testCase.OrderIndex))
        {
            var existing = exercise.TestCases.FirstOrDefault(testCase => testCase.OrderIndex == testCaseSeed.OrderIndex);
            if (existing is null)
            {
                exercise.TestCases.Add(new ExerciseTestCase
                {
                    Input = testCaseSeed.Input,
                    ExpectedOutput = testCaseSeed.ExpectedOutput,
                    IsHidden = testCaseSeed.IsHidden,
                    OrderIndex = testCaseSeed.OrderIndex,
                });
                continue;
            }

            existing.Input = testCaseSeed.Input;
            existing.ExpectedOutput = testCaseSeed.ExpectedOutput;
            existing.IsHidden = testCaseSeed.IsHidden;
            existing.OrderIndex = testCaseSeed.OrderIndex;
        }
    }

    private static IReadOnlyList<ExerciseSeed> GetSeeds()
    {
        return
        [
            BuildSumSeed(),
            BuildFactorialSeed(),
            BuildSortSeed(),
        ];
    }

    private static ExerciseSeed BuildSumSeed()
    {
        return new ExerciseSeed(
            Title: "Divu skaitļu summa",
            Description: "Nolasi divus veselus skaitļus (katrs savā rindā) un izvadi to summu.",
            LanguageCode: "python",
            LanguageVersion: "3.11",
            Difficulty: "Viegls",
            SortOrder: 1,
            TestCases: [
                Tc("3\n5", "8", false, 0),
                Tc("10\n20", "30", false, 1),
                Tc("-5\n3", "-2", true, 2),
                Tc("0\n0", "0", true, 3),
                Tc("1000000\n999999", "1999999", true, 4),
                Tc("-100\n-200", "-300", true, 5),
                Tc("7\n-2", "5", true, 6),
                Tc("-7\n-3", "-10", true, 7),
                Tc("42\n0", "42", true, 8),
                Tc("1\n999", "1000", true, 9),
                Tc("12345\n67890", "80235", true, 10),
                Tc("-50\n100", "50", true, 11),
                Tc("999999999\n1", "1000000000", true, 12),
                Tc("15\n15", "30", true, 13),
                Tc("2\n2", "4", true, 14),
                Tc("-1\n1", "0", true, 15),
                Tc("50\n-25", "25", true, 16),
                Tc("-999999\n999999", "0", true, 17),
                Tc("17\n33", "50", true, 18),
                Tc("8\n12", "20", true, 19),
            ]);
    }

    private static ExerciseSeed BuildFactorialSeed()
    {
        return new ExerciseSeed(
            Title: "Faktoriāls",
            Description: "Nolasi veselu nenegatīvu skaitli n (0 <= n <= 12) un izvadi n! (n faktoriālu).\n\nAtgādinājums: 0! = 1",
            LanguageCode: "python",
            LanguageVersion: "3.11",
            Difficulty: "Viegls",
            SortOrder: 2,
            TestCases: [
                Tc("5", "120", false, 0),
                Tc("3", "6", false, 1),
                Tc("0", "1", true, 2),
                Tc("2", "2", true, 3),
                Tc("3", "6", true, 4),
                Tc("4", "24", true, 5),
                Tc("6", "720", true, 6),
                Tc("7", "5040", true, 7),
                Tc("8", "40320", true, 8),
                Tc("9", "362880", true, 9),
                Tc("10", "3628800", true, 10),
                Tc("11", "39916800", true, 11),
                Tc("12", "479001600", true, 12),
                Tc("2", "2", true, 13),
                Tc("3", "6", true, 14),
                Tc("4", "24", true, 15),
                Tc("6", "720", true, 16),
                Tc("8", "40320", true, 17),
                Tc("10", "3628800", true, 18),
                Tc("12", "479001600", true, 19),
            ]);
    }

    private static ExerciseSeed BuildSortSeed()
    {
        return new ExerciseSeed(
            Title: "Kārto skaitļus",
            Description: "Nolasi veselu skaitļu virkni vienā rindā, sakārto tos augošā secībā un izvadi ar atstarpēm.",
            LanguageCode: "python",
            LanguageVersion: "3.11",
            Difficulty: "Vidējs",
            SortOrder: 3,
            TestCases: [
                Tc("5 1 9 2 7", "1 2 5 7 9", false, 0),
                Tc("8 3 8 1 9", "1 3 8 8 9", false, 1),
                Tc("2 1", "1 2", true, 2),
                Tc("-3 7 0", "-3 0 7", true, 3),
                Tc("10 9 8 7 6", "6 7 8 9 10", true, 4),
                Tc("4 4 2 2 1", "1 2 2 4 4", true, 5),
                Tc("0 -1 1 0", "-1 0 0 1", true, 6),
                Tc("100 20 300 40", "20 40 100 300", true, 7),
                Tc("-10 -20 -5", "-20 -10 -5", true, 8),
                Tc("8 3 8 1 9", "1 3 8 8 9", true, 9),
                Tc("15 14 13 12 11 10", "10 11 12 13 14 15", true, 10),
                Tc("5 0 -5 10 -10", "-10 -5 0 5 10", true, 11),
                Tc("99 98 97 96 95", "95 96 97 98 99", true, 12),
                Tc("1 1 1 1", "1 1 1 1", true, 13),
                Tc("50 -50 25 -25", "-50 -25 25 50", true, 14),
                Tc("3 2 1 0 -1 -2", "-2 -1 0 1 2 3", true, 15),
                Tc("42 17 23 17 42", "17 17 23 42 42", true, 16),
                Tc("6 5 4 3 2 1", "1 2 3 4 5 6", true, 17),
                Tc("12 11 12 11", "11 11 12 12", true, 18),
                Tc("7 0 7 0 7 0", "0 0 0 7 7 7", true, 19),
            ]);
    }

    private static TestCaseSeed Tc(string input, string expectedOutput, bool isHidden, int orderIndex)
    {
        return new TestCaseSeed(input, expectedOutput, isHidden, orderIndex);
    }

    private sealed record ExerciseSeed(
        string Title,
        string Description,
        string LanguageCode,
        string LanguageVersion,
        string Difficulty,
        int SortOrder,
        IReadOnlyList<TestCaseSeed> TestCases);

    private sealed record TestCaseSeed(
        string Input,
        string ExpectedOutput,
        bool IsHidden,
        int OrderIndex);
}
