all:
	dotnet publish -r linux-arm --configuration Release
	dotnet publish -r linux-x64 --configuration Release
	ssh pi@raspberrypi 'rm /home/pi/CBUpdates/*'
	scp bin/Release/netcoreapp5.0/linux-arm/publish/CarrotBot* pi@raspberrypi:/home/pi/CBUpdates
	dotnet publish -r linux-arm --configuration Release ../CBRunningCheck/CBRunningCheck.csproj
	scp ../CBRunningCheck/bin/Release/net5.0/linux-arm/publish/CB* pi@raspberrypi:/home/pi/CBUpdates
update-now:
	make all
	ssh pi@raspberrypi 'sudo reboot'
