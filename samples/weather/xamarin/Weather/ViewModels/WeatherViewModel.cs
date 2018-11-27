using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.MobCAT;
using Microsoft.MobCAT.MVVM;
using Weather.Services.Abstractions;
using Xamarin.Essentials;
using System.Linq;
using System.Threading;
using Weather.Models;
using Microsoft.MobCAT.Services;
using Xamarin.Forms;
using System.Collections.Generic;

namespace Weather.ViewModels
{
    public class WeatherViewModel : BaseNavigationViewModel
    {
        string _cityName;
        string _weatherDescription;
        string _weatherImage;
        string _currentTemp;
        string _highTemp;
        string _lowTemp;
        string _time;
        string _weatherIcon;
        bool _isCelsius;

        IForecastsService forecastsService;
        IImageService imageService;
        IGeolocationService geolocationService;
        IGeocodingService geocodingService;
        readonly Lazy<ITimeOfDayImageService> timeOfDayImageService = new Lazy<ITimeOfDayImageService>(() =>
        {
            try
            {
                return ServiceContainer.Resolve<ITimeOfDayImageService>();
            }
            catch (Exception ex)
            {
                //Previewer or unregistered
            }
            return null;
        });

        Timer _timer;

        public WeatherViewModel()
        {
            IsCelsius = true;

            if (DesignMode.IsDesignModeEnabled)
            {
                CityName = "London";
                IsCelsius = true;
                WeatherDescription = "Cloudy";
                CurrentTemp = "17";
                HighTemp = "20";
                LowTemp = "10";
                WeatherImage = $"https://upload.wikimedia.org/wikipedia/commons/8/82/London_Big_Ben_Phone_box.jpg";
            }

            // Timer to update time
            _timer = new Timer((state) => Time = DateTime.Now.ToShortTimeString(), state: null, dueTime: 100, period: 10000);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            _timer.Dispose();
        }


        public string CityName
        {
            get { return _cityName; }
            set
            {
                RaiseAndUpdate(ref _cityName, value);
            }
        }

        public string CurrentTemp
        {
            get { return _currentTemp; }
            set
            {
                RaiseAndUpdate(ref _currentTemp, value);
            }
        }

        public string HighTemp
        {
            get { return _highTemp; }
            set
            {
                RaiseAndUpdate(ref _highTemp, value);
            }
        }

        public string LowTemp
        {
            get { return _lowTemp; }
            set
            {
                RaiseAndUpdate(ref _lowTemp, value);
            }
        }

        public string WeatherDescription
        {
            get { return _weatherDescription; }
            set
            {
                RaiseAndUpdate(ref _weatherDescription, value);
            }
        }

        public string BackgroundImage => timeOfDayImageService.Value?.GetImageForDateTime(DateTime.Now);

        public string WeatherImage
        {
            get => _weatherImage;
            set => RaiseAndUpdate(ref _weatherImage, value);
        }

        public bool IsCelsius
        {
            get { return _isCelsius; }
            set
            {
                RaiseAndUpdate(ref _isCelsius, value);
            }
        }

        public string TempSymbol
        {
            get { return IsCelsius ? "°C" : "°F"; }
        }

        public string Time
        {
            get { return _time; }
            set
            {
                RaiseAndUpdate(ref _time, value);
            }
        }

        public string WeatherIcon
        {
            get { return _weatherIcon; }
            set
            {
                RaiseAndUpdate(ref _weatherIcon, value);
            }
        }

        public async override Task InitAsync()
        {
            LoadWeatherState(); //load the saved weather state first

            forecastsService = ServiceContainer.Resolve<IForecastsService>();
            imageService = ServiceContainer.Resolve<IImageService>();
            geolocationService = ServiceContainer.Resolve<IGeolocationService>();
            geocodingService = ServiceContainer.Resolve<IGeocodingService>();

            try
            {
                // Use last known location for quicker response
                var location = await geolocationService.GetLastKnownLocationAsync();
                if (location == null)
                {
                    location = await geolocationService.GetLocationAsync();
                }

                if (location != null)
                {
                    var place = await geocodingService.GetPlacesAsync(location);
                    string city = place.FirstOrDefault()?.CityName;

                    CityName = city;

                    var forecast = await forecastsService.GetForecastAsync(city);

                    if (forecast != null)
                    {
                        WeatherDescription = forecast.Overview;
                        WeatherIcon = WeatherIcons.Lookup(WeatherDescription);
                        CurrentTemp = forecast.CurrentTemperature;
                        HighTemp = forecast.MaxTemperature;
                        LowTemp = forecast.MinTemperature;
                        WeatherImage = await imageService.GetImageAsync(city, forecast.Overview);
                    }
                }

                SaveWeatherState();
            }
            catch (FeatureNotSupportedException fnsEx)
            {
                // Handle not supported on device exception
                CityName = "Unable to retrieve location - Feature not supported";
            }
            catch (PermissionException pEx)
            {
                // Handle permission exception
                CityName = "Unable to retrieve location - Need permission";
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                // Unable to get location
            }
            finally
            {
                //Use cached weather image as fallback if necessary
                if (string.IsNullOrEmpty(WeatherImage))
                {
                    WeatherImage = Preferences.Get(Constants.CacheKeys.WeatherImageKey, default(string));
                }
            }
        }

        private void SaveWeatherState()
        {
            Preferences.Set(Constants.CacheKeys.CacheSavedDateTimeKey, DateTime.Now);
            Preferences.Set(Constants.CacheKeys.CityNameKey, CityName);
            Preferences.Set(Constants.CacheKeys.CurrentTempKey, CurrentTemp);
            Preferences.Set(Constants.CacheKeys.HighTempKey, HighTemp);
            Preferences.Set(Constants.CacheKeys.LowTempKey, LowTemp);
            Preferences.Set(Constants.CacheKeys.WeatherDescriptionKey, WeatherDescription);
            Preferences.Set(Constants.CacheKeys.WeatherImageKey, WeatherImage);
            Preferences.Set(Constants.CacheKeys.WeatherIconKey, WeatherIcon);
            Preferences.Set(Constants.CacheKeys.IsCelsiusKey, IsCelsius);
        }

        private void LoadWeatherState()
        {
            var cacheSavedDateTime = Preferences.Get(Constants.CacheKeys.CacheSavedDateTimeKey, default(DateTime));
            if ((DateTime.Now - cacheSavedDateTime).TotalDays < 1) //Only load if it's been less than a day
            {
                CityName = Preferences.Get(Constants.CacheKeys.CityNameKey, default(string));
                CurrentTemp = Preferences.Get(Constants.CacheKeys.CurrentTempKey, default(string));
                HighTemp = Preferences.Get(Constants.CacheKeys.HighTempKey, default(string));
                LowTemp = Preferences.Get(Constants.CacheKeys.LowTempKey, default(string));
                WeatherDescription = Preferences.Get(Constants.CacheKeys.WeatherDescriptionKey, default(string));
                WeatherIcon = Preferences.Get(Constants.CacheKeys.WeatherIconKey, default(string));
                IsCelsius = Preferences.Get(Constants.CacheKeys.IsCelsiusKey, default(bool));
            }
        }
    }
}
