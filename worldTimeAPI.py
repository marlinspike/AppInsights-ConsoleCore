import requests
import random
import sys
import json

timezones = {
    "paris": "http://worldtimeapi.org/api/timezone/Europe/Paris",
    "berlin": "http://worldtimeapi.org/api/timezone/Europe/Berlin",
    "barcelona": "http://worldtimeapi.org/api/timezone/Europe/Barcelona",
    "buenosaires": "http://worldtimeapi.org/api/timezone/America/Argentina/Buenos_Aires",
    "tokyo": "http://worldtimeapi.org/api/timezone/Asia/Tokyo",
    "hobart": "http://worldtimeapi.org/api/timezone/Australia/Hobart",
    "johannesburg": "http://worldtimeapi.org/api/timezone/Africa/Johannesburg",
    "cairo": "http://worldtimeapi.org/api/timezone/Africa/Cairo",
    "newyork": "http://worldtimeapi.org/api/timezone/America/New_York",
    "faroe": "http://worldtimeapi.org/api/timezone/Atlantic/Faroe"
}
def get_timezone(arg):
    city, tz = random.choice(list(timezones.items()))
    #r = requests.get(url=tz)
    #data = r.json()
    
    #print(f"{data['timezone']}: (UTC-Offset {data['utc_offset']}) - {data['datetime']}")
    print(json.dumps({"City": city, "Tz":tz}))
    return(json.dumps({"City": city, "Tz": tz}))
if __name__ == "__main__":
    get_timezone(sys.argv[0])
