## Kerbal Weather Project 
<p align="center">
  <img width="800" height="450" src="Figures/olrtoa_hrly.gif">
</p>

Kerbal Space Program (KSP) is a popular space-flight simulation video game that has been used as a creative sandbox to promote and teach STEM concepts ([Manley, 2016](https://www.youtube.com/watch?v=ogC6ds81gek)). While the game simulates atmospheres with variable depths and densities it lacks dynamic weather limiting its utility as tool for exploring atmospheric science concepts. To remedy this, Kerbal weather Project (KWP) was developed. In KWP, weather and climate data from a global circulation model ([MPAS](https://mpas-dev.github.io/)) were incorporated into KSP gameplay through a C# plugin. More information about KWP is available at the [mod webpage](https://kerbalwxproject.space). The science behind KWP, as well as its potential as an educational tool, was presented at the 101st Annual Meeting of the [American Meteorological Society (AMS)](https://www.ametsoc.org/ams/) in January 2021. A link to the AMS poster presentation on KWP is provided [here](https://32d23e15-c77a-4582-99ed-e8e2b629f64b.usrfiles.com/ugd/32d23e_e83df0eec174495da82aaacd37a9eb51.pdf).

### API Documentation
* https://kerbalweatherproject.readthedocs.io/

### Installation

#### Required Mods
* [Click Through Blocker](https://forum.kerbalspaceprogram.com/index.php?/topic/170747-19x-110x-click-through-blocker-new-dependency/)
* [Toolbar Controller](https://github.com/linuxgurugamer/ToolbarControl)
* [ModularFlightIntegrator](https://ksp.sarbian.com/jenkins/job/ModularFlightIntegrator/33/artifact/ModularFlightIntegrator-1.2.7.0.zip)

#### Recommended Mods

* [Kerbinside Remastered](https://github.com/Eskandare/KerbinSideRemastered/releases/tag/v0.90.1.1) - adds launch sites compataible with KWP and missions for flying around Kerbin
* [KerBalloons](https://forum.kerbalspaceprogram.com/index.php?/topic/199372-18-19-110-kerballoons-reinflated-real-science-beta/) - Enables weather balloons!
* [Atmospheric Autopilot](https://github.com/Boris-Barboris/AtmosphereAutopilot) - makes takeoff/landing during windy conditions easier.
 
##### Manual Installation

1. Download and install [Click Through Blocker](https://forum.kerbalspaceprogram.com/index.php?/topic/170747-19x-110x-click-through-blocker-new-dependency)
2. Download and install [Toolbar Controller](https://github.com/linuxgurugamer/ToolbarControl)
3. Download and install [ModularFlightIntegrator](https://ksp.sarbian.com/jenkins/job/ModularFlightIntegrator/33/artifact/ModularFlightIntegrator-1.2.7.0.zip)
4. Download the repository as a zip or clone it.
5. Unzip the repository and copy the KerbalWeatherProject folder to your KSP home directory (i.e. GameData folder).

##### Automatic Installation (CKAN)

Using CKAN select Kerbal Weather Project and click install! CKAN should automatically install the two dependencies of the mod: toolbar controller and modular fight integrator. If you'd like to emulate the collection of [real-world weather data](https://www.weather.gov/upperair/factsheet) you can also select [KerBalloons](https://forum.kerbalspaceprogram.com/index.php?/topic/199372-18-19-110-kerballoons-reinflated-real-science-beta/) in CKAN. This repository provides a wide array of balloons capable of lifting both sensors and Kerbals!

### Compatability

KWP works with stock KSP and should work with the vast majority of KSP mods. KWP has been tested, without issue, in a KSP 1.10 playthrough with 220 other popular KSP mods. That said, KWP may conflict with mods like [real heat](https://forum.kerbalspaceprogram.com/index.php?/topic/115066-113-realheat-minimalist-v43-july-3/) that modify the stock game's aerodynamic or thermodynamic system. KWP can still be used with these mods as KWP's override of the stock thermodynamic system can be disabled in the settings menu. 

KWP is compatible with the aerodynamics overhaul: Ferram Aerospace Research ([FAR](https://github.com/dkavolis/Ferram-Aerospace-Research)). Note that since FAR overrides KSP's thermodynamic system, pressure and temperature data from KWP will not affect flight dynamics when FAR is installed.

### Background
Kerbin weather and climate analyses were produced using the [Model for Prediction Across Scale](https://mpas-dev.github.io) (MPAS; [Skamarock et al., 2012](https://doi.org/10.1175/MWR-D-11-00215.1))
<p align="center">
  <img width="800" height="430" src="Figures/MPAS_Mesh.png">
</p>
MPAS was run for six-years (1st year: spin-up) at a resolution of 2 x 2 decimal degrees. Fortunately, [Kerbin’s atmosphere](https://wiki.kerbalspaceprogram.com/wiki/Kerbin#Atmosphere) has the same chemical composition as Earth’s.

In MPAS, terrain and biome data from KSP were used to classify land use, vegetation type, green fraction, etc.

In addition to land surface modifications, several changes to MPAS were made to enable more realistic simulations of Kerbin's atmosphere. These changes are listed below:
1. Axial obliquity was set to zero.
1. Orbital eccentricity was set to zero.
1. The solar constant was set to 1360 W/m<sup>2</sup>
1. The day length was set to 6 hours (21600 s)
1. The Coriolis parameter was multiplied by 4.

A climatology of Kerbin was developed by averaging the results of the five-year MPAS simulation, by the hour. Results from this simulation were incorporated into the game via KWP. The hourly climatology allows players to experience diurnal and spatial variations in atmospheric conditions Alternatively, weather time series, extracted at select launch sites, allow players to experience dynamic weather conditions.

### Settings
<p align="center">
  <img width="900" height="550" src="Figures/KSP_Settings.PNG">
</p>

On the settings page, KWP parameters and defaults can be adjusted. Under weather settings, the default data source can be selected as either climatology or point weather data. The MPAS climatology ensures weather conditions will vary spatially and diurnally. In contrast, point weather data will allow weather to change in time and height, at selected launch sites. Users can select whether KWP will affect KSP's aerodynamic or thermodynamic models under weather settings. This is useful if using other mods that influence these models.

Under wind settings, KWP includes options for the source of wind data. By default, wind data will be provided by MPAS. As an alternative, a constant wind profile can be selected and tweaked to the player's preferred direction and speed. An additional option, for disabling wind within 50-m of the ground, is provided for players who find landing/takeoff in the presence of wind too challenging.

Since KSP is a game enjoyed around the world, KWP incorporates unit settings that allow players to select their preferred units for meteorological parameters. The units default to S.I. units.

### Acknowledgements

* KSP Developer Squad and KSP creator Felipe Falanghe.
* KSP Modding Community – specifically KSP forum users linuxgurugamer, JoePatrick1, Fengist, DaMichel, NathanKell, and DMagic. These mod developers, by publishing their code publicly, provided a valuable teaching tool which facilitated many of the advancements implemented in KWP. Without their prior work, incorporating weather data into KSP would have been exceedingly difficult.

### References
Manley (2016), Can Kerbal Space Program Really Teach Rocket Science? https://www.youtube.com/watch?v=ogC6ds81gek. Accessed 12 December 2020.

Skamarock, W. C., J. B. Klemp, M. G. Duda, L. D. Fowler, S. Park, and T. D. Ringler, 2012: A Multiscale Nonhydrostatic Atmospheric Model Using Centroidal Voronoi Tesselations and C-Grid Staggering. Mon. Wea. Rev., 140, 3090–3105, https://doi.org/10.1175/MWR-D-11-00215.1. 
