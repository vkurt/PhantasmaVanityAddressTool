using PhantasmaPhoenix.Cryptography;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

public class VanityAddressGenerator
{
    private enum MatchResult { FullMatch, PartialMatch, NoMatch }
    private enum MatchCategory { FullMatch, CaseMatch, PositionMatch, GenericPartialMatch }

    public static void Main(string[] args)
    {
        string? continueChoice;
        do
        {
            Console.Clear();
            Console.WriteLine("Phantasma Vanity Address Generator");
            Console.WriteLine("-----------------------------------");

            string desiredPattern;
            while (true)
            {
                Console.Write("Enter the desired pattern: ");
                desiredPattern = Console.ReadLine() ?? string.Empty;

                if (string.IsNullOrWhiteSpace(desiredPattern))
                {
                    Console.WriteLine("\n❌ Error: Pattern cannot be empty. Please enter a pattern.");
                }
                else if (desiredPattern.Any(c => char.IsWhiteSpace(c) || !char.IsLetterOrDigit(c)))
                {
                    Console.WriteLine("\n❌ Error: Pattern cannot contain spaces or special characters. Please use only letters and numbers.");
                }
                else
                {
                    break;
                }
            }
            
            string caseChoice;
            while (true)
            {
                Console.WriteLine("\nChoose case sensitivity (Default: 1):");
                Console.WriteLine("1. Case-insensitive (Fast)");
                Console.WriteLine("2. Case-sensitive (Slow)");
                Console.Write("Enter your choice (1 or 2): ");
                caseChoice = Console.ReadLine() ?? "1";
                if (string.IsNullOrWhiteSpace(caseChoice)) caseChoice = "1";
                if (caseChoice == "1" || caseChoice == "2")
                {
                    break;
                }
                Console.WriteLine("\nInvalid choice. Please enter 1 or 2.");
            }
            bool isCaseSensitive = (caseChoice == "2");

            string positionChoice;
            while (true)
            {
                Console.WriteLine("\nChoose pattern position (Default: 1):");
                Console.WriteLine("1. Anywhere (Fast)");
                Console.WriteLine("2. At the end (Slow)");
                Console.Write("Enter your choice (1 or 2): ");
                positionChoice = Console.ReadLine() ?? "1";
                if (string.IsNullOrWhiteSpace(positionChoice)) positionChoice = "1";
                if (positionChoice == "1" || positionChoice == "2")
                {
                    break;
                }
                Console.WriteLine("\nInvalid choice. Please enter 1 or 2.");
            }
            bool isAtEnd = (positionChoice == "2");

            Console.Write("\nEnter a give-up time in minutes (0 for no time limit) (Default: 0): ");
            string giveUpInput = Console.ReadLine() ?? "0";
            if (string.IsNullOrWhiteSpace(giveUpInput)) giveUpInput = "0";
            int giveUpMinutes;
            while (!int.TryParse(giveUpInput, out giveUpMinutes) || giveUpMinutes < 0)
            {
                Console.Write("Invalid input. Please enter a positive integer: ");
                giveUpInput = Console.ReadLine() ?? "0";
                if (string.IsNullOrWhiteSpace(giveUpInput)) giveUpInput = "0";
            }
            TimeSpan giveUpTime = TimeSpan.FromMinutes(giveUpMinutes);
            
            string matchesInput;
            int desiredMatches;
            while (true)
            {
                Console.Write("\nEnter the number of full matches you want to find (Default: 1): ");
                matchesInput = Console.ReadLine() ?? "1";
                if (string.IsNullOrWhiteSpace(matchesInput)) matchesInput = "1";

                if (int.TryParse(matchesInput, out desiredMatches) && desiredMatches > 0)
                {
                    break;
                }
                Console.WriteLine("\nInvalid input. Please enter a positive integer.");
            }
            
            string genChoice;
            while (true)
            {
                Console.WriteLine("\nChoose a generation method (Default: 1):");
                Console.WriteLine("1. Fast (Generate from random private key)");
                Console.WriteLine("2. Slow (Generate from a random seed phrase)");
                Console.WriteLine("3. Smart (Generate from a single new seed phrase, changing index)");
                Console.WriteLine("4. User (Find an address on a specific seed phrase, changing index)");
                Console.WriteLine("\n⚠️⚠️⚠️ For 3rd and 4th options be aware 99% of wallets not supports manually entering index number and if you lost index number it can be impossible to recover your assets please save your wif too.");
                Console.Write("\nEnter your choice (1, 2, 3, or 4): ");
                genChoice = Console.ReadLine() ?? "1";
                if (string.IsNullOrWhiteSpace(genChoice)) genChoice = "1";
                if (genChoice == "1" || genChoice == "2" || genChoice == "3" || genChoice == "4")
                {
                    break;
                }
                Console.WriteLine("\nInvalid choice. Please enter 1, 2, 3, or 4.");
            }
            
            string savePartialMatchesChoice;
            while (true)
            {
                Console.WriteLine("\nSave partial matches to file? (y/n) (Default: y):");
                Console.Write("Enter your choice: ");
                savePartialMatchesChoice = Console.ReadLine() ?? "y";
                if (string.IsNullOrWhiteSpace(savePartialMatchesChoice)) savePartialMatchesChoice = "y";
                if (savePartialMatchesChoice.ToLower() == "y" || savePartialMatchesChoice.ToLower() == "n")
                {
                    break;
                }
                Console.WriteLine("\nInvalid choice. Please enter 'y' or 'n'.");
            }
            bool savePartialMatches = savePartialMatchesChoice.ToLower() == "y";

            bool wasCanceled = false;

            switch (genChoice)
            {
                case "1":
                    wasCanceled = !GenerateFromKeyPair(desiredPattern, isCaseSensitive, isAtEnd, giveUpTime, savePartialMatches, desiredMatches);
                    break;
                case "2":
                    wasCanceled = !GenerateFromSeedPhrase(desiredPattern, isCaseSensitive, isAtEnd, giveUpTime, savePartialMatches, desiredMatches);
                    break;
                case "3":
                    wasCanceled = !GenerateFromSeedWithIndex(desiredPattern, isCaseSensitive, isAtEnd, giveUpTime, savePartialMatches, desiredMatches);
                    break;
                case "4":
                    wasCanceled = !GenerateFromUserSeedWithIndex(desiredPattern, isCaseSensitive, isAtEnd, giveUpTime, savePartialMatches, desiredMatches);
                    break;
            }

            if (wasCanceled)
            {
                Console.WriteLine("\nSearch was canceled.");
            }

            Console.Write("\nDo you want to run another search? (y/n): ");
            continueChoice = Console.ReadLine();

        } while (continueChoice?.ToLower() == "y");

        Console.WriteLine("Program exiting. Press any key to close...");
        Console.ReadKey();
    }
    
    //-----------------------------------------------------------------------------------------
    // Method 1: Generate from random private key
    //-----------------------------------------------------------------------------------------
    public static bool GenerateFromKeyPair(string desiredPattern, bool isCaseSensitive, bool isAtEnd, TimeSpan giveUpTime, bool savePartialMatches, int desiredMatches)
    {
        Console.WriteLine("\nStarting fast generation...");
        long attempts = 0;
        int foundFullMatches = 0;
        int foundCaseMatches = 0;
        int foundPositionMatches = 0;
        int foundGenericMatches = 0;
        var stopwatch = Stopwatch.StartNew();
        
        while (true)
        {
            var keyPair = PhantasmaKeys.Generate();
            var address = keyPair.Address.Text;
            attempts++;
            if (!CheckTimerAndInput(attempts, stopwatch, giveUpTime, foundFullMatches, desiredMatches, foundCaseMatches, foundPositionMatches, foundGenericMatches)) return false;
            
            MatchCategory matchCategory = CategorizeMatch(address, desiredPattern, isCaseSensitive, isAtEnd);

            switch (matchCategory)
            {
                case MatchCategory.FullMatch:
                    Console.WriteLine("\n\n🎉 Success! Found a full-match vanity address!");
                    Console.WriteLine($"Matching Address: {address}");
                    Console.WriteLine($"Private Key: {keyPair.ToWIF()}");
                    SaveMatchToFile(desiredPattern, address, keyPair.ToWIF(), null, true, matchCategory);
                    foundFullMatches++;
                    if (foundFullMatches >= desiredMatches)
                    {
                        stopwatch.Stop();
                        return true;
                    }
                    break;
                case MatchCategory.CaseMatch:
                    foundCaseMatches++;
                    if (savePartialMatches)
                    {
                        Console.WriteLine($"\n\n✅ {matchCategory} found: {address}");
                        SaveMatchToFile(desiredPattern, address, keyPair.ToWIF(), null, false, matchCategory);
                    }
                    break;
                case MatchCategory.PositionMatch:
                    foundPositionMatches++;
                    if (savePartialMatches)
                    {
                        Console.WriteLine($"\n\n✅ {matchCategory} found: {address}");
                        SaveMatchToFile(desiredPattern, address, keyPair.ToWIF(), null, false, matchCategory);
                    }
                    break;
                case MatchCategory.GenericPartialMatch:
                    foundGenericMatches++;
                    break;
            }
        }
    }

    //-----------------------------------------------------------------------------------------
    // Method 2: Generate from random seed phrase
    //-----------------------------------------------------------------------------------------
    public static bool GenerateFromSeedPhrase(string desiredPattern, bool isCaseSensitive, bool isAtEnd, TimeSpan giveUpTime, bool savePartialMatches, int desiredMatches)
    {
        Console.WriteLine("\nStarting slow generation...");
        long attempts = 0;
        int foundFullMatches = 0;
        int foundCaseMatches = 0;
        int foundPositionMatches = 0;
        int foundGenericMatches = 0;
        var stopwatch = Stopwatch.StartNew();

        while (true)
        {
            string seedPhrase = Mnemonics.GenerateMnemonic(MnemonicPhraseLength.Twelve_Words);

            var (pk, errorMessage) = Mnemonics.MnemonicToPK(seedPhrase);

            if (pk == null || errorMessage != null)
            {
                Console.WriteLine($"Error occured: {errorMessage}");
                continue;
            }

            var keyPair = new PhantasmaKeys(pk);
            var address = keyPair.Address.Text;
            attempts++;
            if (!CheckTimerAndInput(attempts, stopwatch, giveUpTime, foundFullMatches, desiredMatches, foundCaseMatches, foundPositionMatches, foundGenericMatches)) return false;
            MatchCategory matchCategory = CategorizeMatch(address, desiredPattern, isCaseSensitive, isAtEnd);

            switch (matchCategory)
            {
                case MatchCategory.FullMatch:
                    Console.WriteLine("\n\n🎉 Success! Found a full-match vanity address!");
                    Console.WriteLine($"Matching Address: {address}");
                    Console.WriteLine($"Seed Phrase: {seedPhrase}");
                    SaveMatchToFile(desiredPattern, address, keyPair.ToWIF(), seedPhrase, true, matchCategory);
                    foundFullMatches++;
                    if (foundFullMatches >= desiredMatches)
                    {
                        stopwatch.Stop();
                        return true;
                    }
                    break;
                case MatchCategory.CaseMatch:
                    foundCaseMatches++;
                    if (savePartialMatches)
                    {
                        Console.WriteLine($"\n\n✅ {matchCategory} found: {address}");
                        SaveMatchToFile(desiredPattern, address, keyPair.ToWIF(), seedPhrase, false, matchCategory);
                    }
                    break;
                case MatchCategory.PositionMatch:
                    foundPositionMatches++;
                    if (savePartialMatches)
                    {
                        Console.WriteLine($"\n\n✅ {matchCategory} found: {address}");
                        SaveMatchToFile(desiredPattern, address, keyPair.ToWIF(), seedPhrase, false, matchCategory);
                    }
                    break;
                case MatchCategory.GenericPartialMatch:
                    foundGenericMatches++;
                    break;
            }
        }
    }
    
    //-----------------------------------------------------------------------------------------
    // Method 3: Generate from a single new seed phrase, changing index
    //-----------------------------------------------------------------------------------------
    public static bool GenerateFromSeedWithIndex(string desiredPattern, bool isCaseSensitive, bool isAtEnd, TimeSpan giveUpTime, bool savePartialMatches, int desiredMatches)
    {
        Console.WriteLine("\nStarting smart generation...");
        string seedPhrase = Mnemonics.GenerateMnemonic(MnemonicPhraseLength.Twelve_Words);
        Console.WriteLine($"\nBase Seed Phrase: {seedPhrase}");
        Console.WriteLine("Searching for matching address by incrementing index...");
        uint attempts = 0;
        int foundFullMatches = 0;
        int foundCaseMatches = 0;
        int foundPositionMatches = 0;
        int foundGenericMatches = 0;
        var stopwatch = Stopwatch.StartNew();
        
        while (true)
        {
            var (pk, errorMessage) = Mnemonics.MnemonicToPK(seedPhrase, attempts);

            if (pk == null || errorMessage != null)
            {
                Console.WriteLine($"Error occured: {errorMessage}");
                continue;
            }

            var keyPair = new PhantasmaKeys(pk);
            var address = keyPair.Address.Text;
            attempts++;
            if (!CheckTimerAndInput(attempts, stopwatch, giveUpTime, foundFullMatches, desiredMatches, foundCaseMatches, foundPositionMatches, foundGenericMatches)) return false;
            MatchCategory matchCategory = CategorizeMatch(address, desiredPattern, isCaseSensitive, isAtEnd);

            switch (matchCategory)
            {
                case MatchCategory.FullMatch:
                    Console.WriteLine("\n\n🎉 Success! Found a full-match vanity address!");
                    Console.WriteLine($"Matching Address: {address}");
                    Console.WriteLine($"Derived from Index: {attempts - 1}");
                    Console.WriteLine($"Original Seed Phrase: {seedPhrase}");
                    SaveMatchToFile(desiredPattern, address, keyPair.ToWIF(), seedPhrase, true, matchCategory, attempts - 1);
                    foundFullMatches++;
                    if (foundFullMatches >= desiredMatches)
                    {
                        stopwatch.Stop();
                        return true;
                    }
                    break;
                case MatchCategory.CaseMatch:
                    foundCaseMatches++;
                    if (savePartialMatches)
                    {
                        Console.WriteLine($"\n\n✅ {matchCategory} found: {address}");
                        SaveMatchToFile(desiredPattern, address, keyPair.ToWIF(), seedPhrase, false, matchCategory, attempts - 1);
                    }
                    break;
                case MatchCategory.PositionMatch:
                    foundPositionMatches++;
                    if (savePartialMatches)
                    {
                        Console.WriteLine($"\n\n✅ {matchCategory} found: {address}");
                        SaveMatchToFile(desiredPattern, address, keyPair.ToWIF(), seedPhrase, false, matchCategory, attempts - 1);
                    }
                    break;
                case MatchCategory.GenericPartialMatch:
                    foundGenericMatches++;
                    break;
            }

            if (attempts == 4294967295)
            {
                Console.WriteLine("Reached maximum attempts try again with shorter pattern or select fast options.");
                return true;
            }
        }
    }
    
    //-----------------------------------------------------------------------------------------
    // Method 4: Find an address on a user-provided seed phrase, changing index
    //-----------------------------------------------------------------------------------------
    public static bool GenerateFromUserSeedWithIndex(string desiredPattern, bool isCaseSensitive, bool isAtEnd, TimeSpan giveUpTime, bool savePartialMatches, int desiredMatches)
    {
        Console.WriteLine("\n⚠️  WARNING: Enter your seed phrase ONLY on an offline computer.");
        Console.Write("Enter your 12 or 24-word seed phrase (separated by spaces): ");
        string userSeedPhrase = Console.ReadLine() ?? string.Empty;
        Console.WriteLine($"\nSearching for matching address from your seed phrase...");
        uint attempts = 0;
        int foundFullMatches = 0;
        int foundCaseMatches = 0;
        int foundPositionMatches = 0;
        int foundGenericMatches = 0;
        var stopwatch = Stopwatch.StartNew();

        while (true)
        {
            var (pk, errorMessage) = Mnemonics.MnemonicToPK(userSeedPhrase, attempts);

            if (pk == null || errorMessage != null)
            {
                Console.WriteLine($"Error occured: {errorMessage}");
                return true;
            }
            var keyPair = new PhantasmaKeys(pk);
            var address = keyPair.Address.Text;
            attempts++;
            if (!CheckTimerAndInput(attempts, stopwatch, giveUpTime, foundFullMatches, desiredMatches, foundCaseMatches, foundPositionMatches, foundGenericMatches)) return false;
            MatchCategory matchCategory = CategorizeMatch(address, desiredPattern, isCaseSensitive, isAtEnd);

            switch (matchCategory)
            {
                case MatchCategory.FullMatch:
                    Console.WriteLine("\n\n🎉 Success! Found a full-match vanity address!");
                    Console.WriteLine($"Matching Address: {address}");
                    Console.WriteLine($"Derived from Index: {attempts - 1}");
                    Console.WriteLine($"Original Seed Phrase: {userSeedPhrase}");
                    SaveMatchToFile(desiredPattern, address, keyPair.ToWIF(), userSeedPhrase, true, matchCategory, attempts - 1);
                    foundFullMatches++;
                    if (foundFullMatches >= desiredMatches)
                    {
                        stopwatch.Stop();
                        return true;
                    }
                    break;
                case MatchCategory.CaseMatch:
                    foundCaseMatches++;
                    if (savePartialMatches)
                    {
                        Console.WriteLine($"\n\n✅ {matchCategory} found: {address}");
                        SaveMatchToFile(desiredPattern, address, keyPair.ToWIF(), userSeedPhrase, false, matchCategory, attempts - 1);
                    }
                    break;
                case MatchCategory.PositionMatch:
                    foundPositionMatches++;
                    if (savePartialMatches)
                    {
                        Console.WriteLine($"\n\n✅ {matchCategory} found: {address}");
                        SaveMatchToFile(desiredPattern, address, keyPair.ToWIF(), userSeedPhrase, false, matchCategory, attempts - 1);
                    }
                    break;
                case MatchCategory.GenericPartialMatch:
                    foundGenericMatches++;
                    break;
            }
            
            if (attempts == 4294967295)
            {
                Console.WriteLine("Reached maximum attempts try again with shorter pattern or select fast options.");
                return true;
            }
        }
    }

    //-----------------------------------------------------------------------------------------
    // Helper Functions
    //-----------------------------------------------------------------------------------------
    private static bool CheckTimerAndInput(long attempts, Stopwatch stopwatch, TimeSpan giveUpTime, int foundFullMatches, int desiredFullMatches, int foundCaseMatches, int foundPositionMatches, int foundGenericMatches)
    {
        if (giveUpTime > TimeSpan.Zero && stopwatch.Elapsed > giveUpTime)
        {
            Console.WriteLine("\n\n❌ Timed out. No matching address found.");
            return false;
        }

        if (Console.KeyAvailable)
        {
            var key = Console.ReadKey(true).Key;
            if (key == ConsoleKey.Escape)
            {
                stopwatch.Stop();
                return false;
            }
            if (key == ConsoleKey.P)
            {
                stopwatch.Stop();
                Console.WriteLine("\n\n⏸️  Paused. Press any key to continue...");
                Console.ReadKey(true);
                Console.CursorVisible = true;
                stopwatch.Start();
            }
        }

        if (attempts % 1000 == 0)
        {
            TimeSpan elapsed = stopwatch.Elapsed;
            double attemptsPerSecond = attempts / elapsed.TotalSeconds;
            
            string remainingTime = giveUpTime > TimeSpan.Zero
                ? $" | Remaining: {(giveUpTime - elapsed).ToString(@"hh\:mm\:ss")}"
                : "";
            
            Console.Write($"\rChecked {attempts:N0} | Elapsed: {elapsed.ToString(@"hh\:mm\:ss")} | Speed: {attemptsPerSecond:N2} adds/sec{remainingTime} | Matches: F:{foundFullMatches}/{desiredFullMatches} C:{foundCaseMatches} P:{foundPositionMatches} G:{foundGenericMatches} | Press 'P' for pause, 'Esc' to exit or retry");
        }
        return true;
    }

    private static MatchCategory CategorizeMatch(string address, string pattern, bool isCaseSensitive, bool isAtEnd)
    {
        if (string.IsNullOrWhiteSpace(pattern))
        {
            return MatchCategory.GenericPartialMatch;
        }

        StringComparison strictComparison = isCaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
        StringComparison broadComparison = StringComparison.OrdinalIgnoreCase;

        bool isCaseCorrect = address.IndexOf(pattern, strictComparison) >= 0;
        bool isPositionCorrect = isAtEnd ? address.EndsWith(pattern, broadComparison) : true;
        
        bool broadMatch = address.IndexOf(pattern, broadComparison) >= 0;

        if (!broadMatch)
        {
            return MatchCategory.GenericPartialMatch;
        }

        // Full Match
        if (isCaseCorrect && isPositionCorrect)
        {
            return MatchCategory.FullMatch;
        }
        
        // Partial Match Categories
        if (isCaseCorrect)
        {
            return MatchCategory.CaseMatch;
        }
        if (isPositionCorrect)
        {
            return MatchCategory.PositionMatch;
        }

        return MatchCategory.GenericPartialMatch;
    }
    
    private static void SaveMatchToFile(string desiredPattern, string address, string privateKey, string? seedPhrase, bool isFullMatch, MatchCategory category, long? index = null)
    {
        string baseFolder = "Results";
        
        
        string categoryFolderName = category.ToString();
        string finalFolderPath = Path.Combine(baseFolder, categoryFolderName);
        Directory.CreateDirectory(finalFolderPath);

        string filename = Path.Combine(finalFolderPath, $"{desiredPattern}.txt");
        
        using (StreamWriter writer = new StreamWriter(filename, true)) // 'true' to append
        {
            writer.WriteLine("------------------------------------------");
            writer.WriteLine($"Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            writer.WriteLine($"Match Type: {category}");
            writer.WriteLine($"Address: {address}");
            if (!string.IsNullOrEmpty(privateKey))
            {
                writer.WriteLine($"Private Key: {privateKey}");
            }
            if (!string.IsNullOrEmpty(seedPhrase))
            {
                writer.WriteLine($"Seed Phrase: {seedPhrase}");
            }
            if (index.HasValue)
            {
                writer.WriteLine($"Index: {index.Value}");
            }
            writer.WriteLine("------------------------------------------");
        }
        
        Console.WriteLine($"\n✅ Saved to: {Path.GetFullPath(filename)}");
    }
}
