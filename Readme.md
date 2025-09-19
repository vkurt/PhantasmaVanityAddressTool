# Phantasma Vanity Address Generator

This is an experimental command-line tool designed to generate Phantasma addresses that contain a specific, desired pattern. It's built to assist users in creating personalized addresses for the Phantasma blockchain.

## ⚠️ Disclaimer

This is an experimental tool created with the help of AI. It is provided "as is" and should be used at your own risk. **You must run this tool on an offline machine** and be extremely careful when handling private keys and seed phrases. The developers and contributors are not responsible for any loss of funds.

## Features

* **Pattern Matching**: Search for any alphanumeric pattern within a Phantasma address.

* **Configurable Search**: Choose between case-sensitive or case-insensitive matching and specify if the pattern should be at the end of the address or anywhere within it.

* **Multiple Generation Methods**:

  1. **Fast**: Generates addresses from random private keys.

  2. **Slow**: Generates addresses from random 12-word seed phrases.

  3. **Smart**: Generates addresses from a single new seed phrase by incrementing the index.

  4. **User**: Generates addresses from a user-provided seed phrase by incrementing the index (for finding an address you already know exists on a specific seed).

* **Time-limited Search**: Set a maximum time limit for the search to stop automatically.

* **File Output**: Automatically saves all found addresses, private keys, and seed phrases to a text file for safekeeping.

## How to Use

### Download and Run (Pre-compiled)

For most users, this is the easiest way to use the tool. The executable files are self-contained and do not require the .NET SDK to be installed.

1. Navigate to the [Releases page on GitHub](https://www.google.com/search?q=https://github.com/vkurt/phantasmavanityaddresstool/releases) (or wherever the executables are hosted).

2. Download the appropriate file for your operating system:

   * **Windows**: Download the `phantasma-vanity-generator-win-x64.zip` file, extract it, and run the `VanityAddressGenerator.exe`.

   * **Linux**: Download the `phantasma-vanity-generator-linux-x64.zip` file, extract it, and run the `VanityAddressGenerator` executable.

   * **macOS**: Download the `phantasma-vanity-generator-osx-x64.zip` file, extract it, and run the `VanityAddressGenerator` executable.

### Building from Source

This is recommended for developers or users who want to inspect and compile the code themselves.

#### Prerequisites

You need the .NET SDK installed on your machine. This tool also requires the `PhantasmaPhoenix.Cryptography` library, which should be included in the project dependencies.

#### Running the Tool

1. Compile the C# code using `dotnet build` from your terminal.

2. Run the executable from your `bin/` directory.

The program will guide you through the process with a series of prompts:

1. **Enter the desired pattern:** Type the string you want to appear in your address (e.g., `GHOST`).

2. **Choose case sensitivity:**

   * `1` for Case-insensitive (faster)

   * `2` for Case-sensitive (slower)

3. **Choose pattern position:**

   * `1` for Anywhere

   * `2` for At the end

4. **Enter a give-up time:** Enter a number in minutes (e.g., `10`) or `0` for no time limit.

5. **Enter the number of full matches:** Specify how many matching addresses you want to find before the program stops.

6. **Choose a generation method:** Select one of the four methods listed in the Features section.

7. **Save partial matches:** Choose `y` or `n` to save addresses that partially match your criteria.

### Generated Files

The tool will create a `Results` folder in the same directory as the executable. Inside this folder, it will organize the output into subfolders based on the match type: `FullMatch`, `CaseMatch`, `PositionMatch`, and `GenericPartialMatch`.

Each `txt` file within these subfolders contains the addresses, private keys, and seed phrases found during the search.

Remember to keep your `Results` folder private and secure. The `.gitignore` file I also created will prevent this folder from being accidentally uploaded to your GitHub repository.