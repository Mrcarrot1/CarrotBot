all:
	dotnet publish -r linux-arm --configuration Release
	dotnet publish -r linux-x64 --configuration Release
	ssh pi@raspberrypi 'rm /home/pi/CBUpdates/*'
	scp ~/Documents/CarrotBot/DSharpPlus/CarrotBot/bin/Release/netcoreapp5.0/linux-arm/publish/CarrotBot* pi@raspberrypi:/home/pi/CBUpdates
