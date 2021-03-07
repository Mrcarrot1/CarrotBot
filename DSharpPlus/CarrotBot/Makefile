all:
	dotnet publish -r linux-arm
	dotnet publish -r linux-x64
	ssh pi@raspberrypi 'rm /home/pi/CBUpdates/*'
	scp ~/Documents/CarrotBot/DSharpPlus/CarrotBot/bin/Debug/netcoreapp3.1/linux-arm/publish/CarrotBot* pi@raspberrypi:/home/pi/CBUpdates
