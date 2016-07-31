#region using directives

using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using PoGo.NecroBot.Logic.Common;
using PoGo.NecroBot.Logic.Event;
using PoGo.NecroBot.Logic.Utils;

#endregion

namespace PoGo.NecroBot.Logic.State
{
    public class PositionCheckState : IState
    {
        public async Task<IState> Execute(ISession session)
        {
            var coordsPath = Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + "config" +
                             Path.DirectorySeparatorChar + "coords.ini";
            if (File.Exists(coordsPath))
            {
                var latLngFromFile = LoadPositionFromDisk(session);
                if (latLngFromFile != null)
                {
                    var distance = LocationUtils.CalculateDistanceInMeters(latLngFromFile.Item1, latLngFromFile.Item2,
                        session.Settings.DefaultLatitude, session.Settings.DefaultLongitude);
                    var lastModified = File.Exists(coordsPath) ? (DateTime?) File.GetLastWriteTime(coordsPath) : null;
                    if (lastModified != null)
                    {
                        var hoursSinceModified = (DateTime.Now - lastModified).HasValue
                            ? (double?) ((DateTime.Now - lastModified).Value.Minutes/60.0)
                            : null;
                        if (hoursSinceModified != null && hoursSinceModified != 0)
                        {
                            var kmph = distance/1000/(double) hoursSinceModified;
                            if (kmph < 80) // If speed required to get to the default location is < 80km/hr
                            {
//                                File.Delete(coordsPath);
                                session.EventDispatcher.Send(new WarnEvent
                                {
                                    Message = session.Translation.GetTranslation(TranslationString.RealisticTravelDetected)
                                });
                            }
                            else
                            {
                                //session.Client.CurrentLatitude = lat;
                               // session.Client.CurrentLongitude = lng;
                                session.EventDispatcher.Send(new WarnEvent
                                {
                                    Message =session.Translation.GetTranslation(TranslationString.NotRealisticTravel, kmph)
                                });
                            }
                        }
                        session.Client.Player.SetCoordinates(latLngFromFile.Item1, latLngFromFile.Item2, session.Settings.DefaultAltitude, false);

                        await Task.Delay(200);
                    }
                }
            }

            session.EventDispatcher.Send(new WarnEvent
            {
                Message =
                    session.Translation.GetTranslation(TranslationString.WelcomeWarning, session.Client.CurrentLatitude,
                        session.Client.CurrentLongitude),
                RequireInput = session.LogicSettings.StartupWelcomeDelay
            });

            return new InfoState();
        }

        private static Tuple<double, double> LoadPositionFromDisk(ISession session)
        {
            if (
                File.Exists(Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + "config" +
                            Path.DirectorySeparatorChar + "coords.ini") &&
                File.ReadAllText(Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + "config" +
                                 Path.DirectorySeparatorChar + "coords.ini").Contains(","))
            {
                var latlngFromFile =
                    File.ReadAllText(Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + "config" +
                                     Path.DirectorySeparatorChar + "coords.ini");
                var latlng = latlngFromFile.Split(',');
                if (latlng[0].Length != 0 && latlng[1].Length != 0)
                {
                    try
                    {
                        var latitude = Convert.ToDouble(latlng[0], CultureInfo.InvariantCulture);
                        var longitude = Convert.ToDouble(latlng[1], CultureInfo.InvariantCulture);

                        if (Math.Abs(latitude) <= 90 && Math.Abs(longitude) <= 180)
                        {
                            return new Tuple<double, double>(latitude, longitude);
                        }
                        session.EventDispatcher.Send(new WarnEvent
                        {
                            Message = session.Translation.GetTranslation(TranslationString.CoordinatesAreInvalid)
                        });
                        return null;
                    }
                    catch (FormatException)
                    {
                        session.EventDispatcher.Send(new WarnEvent
                        {
                            Message = session.Translation.GetTranslation(TranslationString.CoordinatesAreInvalid)
                        });
                        return null;
                    }
                }
            }

            return null;
        }
    }
}