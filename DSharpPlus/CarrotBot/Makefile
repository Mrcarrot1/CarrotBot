all:
	dotnet publish -r linux-arm --configuration Release
	dotnet publish -r linux-x64 --configuration Release
	ssh pi@raspberrypi 'rm /home/pi/CBUpdates/*'
	scp bin/Release/netcoreapp5.0/linux-arm/publish/CarrotBot* pi@raspberrypi:/home/pi/CBUpdates
update-now:
	dotnet publish -r linux-arm --configuration Release
	dotnet publish -r linux-x64 --configuration Release
	ssh pi@raspberrypi 'rm /home/pi/CBUpdates/*'
	scp bin/Release/netcoreapp5.0/linux-arm/publish/CarrotBot* pi@raspberrypi:/home/pi/CBUpdates
	ssh pi@raspberrypi 'sudo pkill CarrotBot && sudo rm CarrotBot/Bot/CarrotBot* && cp CBUpdates/* CarrotBot/Bot'
	ssh pi@raspberrypi 'sudo reboot'
