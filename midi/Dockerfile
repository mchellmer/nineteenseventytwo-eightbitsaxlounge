# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files
COPY NineteenSeventyTwo.EightBitSaxLounge.Midi.sln ./
COPY NineteenSeventyTwo.EightBitSaxLounge.Midi/NineteenSeventyTwo.EightBitSaxLounge.Midi.MinimalApi/*.csproj ./NineteenSeventyTwo.EightBitSaxLounge.Midi/NineteenSeventyTwo.EightBitSaxLounge.Midi.MinimalApi/
COPY NineteenSeventyTwo.EightBitSaxLounge.Midi/NineteenSeventyTwo.EightBitSaxLounge.Midi.Library/*.csproj ./NineteenSeventyTwo.EightBitSaxLounge.Midi/NineteenSeventyTwo.EightBitSaxLounge.Midi.Library/
COPY NineteenSeventyTwo.EightBitSaxLounge.Midi/NineteenSeventyTwo.EightBitSaxLounge.Midi.UnitTests/*.csproj ./NineteenSeventyTwo.EightBitSaxLounge.Midi/NineteenSeventyTwo.EightBitSaxLounge.Midi.UnitTests/

# Restore dependencies
RUN dotnet restore

# Copy source code
COPY NineteenSeventyTwo.EightBitSaxLounge.Midi/NineteenSeventyTwo.EightBitSaxLounge.Midi.MinimalApi/ ./NineteenSeventyTwo.EightBitSaxLounge.Midi/NineteenSeventyTwo.EightBitSaxLounge.Midi.MinimalApi/
COPY NineteenSeventyTwo.EightBitSaxLounge.Midi/NineteenSeventyTwo.EightBitSaxLounge.Midi.Library/ ./NineteenSeventyTwo.EightBitSaxLounge.Midi/NineteenSeventyTwo.EightBitSaxLounge.Midi.Library/

# Build
WORKDIR /src/NineteenSeventyTwo.EightBitSaxLounge.Midi/NineteenSeventyTwo.EightBitSaxLounge.Midi.MinimalApi
RUN dotnet publish -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .

# Create non-root user
RUN useradd -m -u 1000 midiuser && chown -R midiuser:midiuser /app
USER midiuser

EXPOSE 8080
ENTRYPOINT ["dotnet", "NineteenSeventyTwo.EightBitSaxLounge.Midi.MinimalApi.dll"]
