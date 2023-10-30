# Meteomatics Weather Data Azure Function

This Azure Function fetches weather data from the Meteomatics API for multiple coordinates and stores the data in Azure Blob Storage. It is designed to run on a timer trigger and can be configured to fetch data at specific intervals.

## Prerequisites

Before deploying and running this function, make sure you have the following:

- An Azure subscription.
- Azure Functions runtime installed locally for development and testing.
- Azure Storage account for storing weather data.
- Meteomatics API credentials (username and password).
- Visual Studio or Visual Studio Code for development (optional).

## Configuration

To configure this function, you need to set the following environment variables in your Azure Function App:

- `AZURE_STORAGE_CONNECTION_STRING`: The connection string for your Azure Storage account.
- `METEOMATICS_USERNAME`: Your Meteomatics API username.
- `METEOMATICS_PASSWORD`: Your Meteomatics API password.

## Usage

1. Clone or download this repository to your local development environment.

2. Open the project in Visual Studio or Visual Studio Code (optional).

3. Configure the required environment variables in your Azure Function App as mentioned in the "Configuration" section.

4. Update the list of coordinates in the code to include the specific locations for which you want to fetch weather data.

5. Deploy the Azure Function to your Azure Function App.

6. The function will run on the specified timer trigger (default: every hour) and fetch weather data for each coordinate.

7. The weather data for each coordinate will be stored in Azure Blob Storage with filenames in the format `<NameId>_MeteomaticsWeatherData.json`.

## Example Coordinates

Here is an example list of coordinates in the `coordinates` list:

```csharp
private static readonly List<Coordinate> coordinates = new List<Coordinate>
{
    new Coordinate { Latitude = 59.02050003940309, Longitude = 5.592325942611728, NameId = "Sande" },
    new Coordinate { Latitude = 58.88531361351894, Longitude = 5.602662428854268, NameId = "Sola" },
    // Add more coordinates as needed
};
