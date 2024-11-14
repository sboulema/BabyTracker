# BabyTracker
Website to view a Data Clone from the [BabyTracker](https://nighp.com/babytracker/) mobile app.

[![BabyTracker](https://github.com/sboulema/BabyTracker/actions/workflows/workflow.yml/badge.svg)](https://github.com/sboulema/BabyTracker/actions/workflows/workflow.yml)
[![Sponsor](https://img.shields.io/badge/-Sponsor-fafbfc?logo=GitHub%20Sponsors)](https://github.com/sponsors/sboulema)

## Screenshots

### Account
[![Login](https://raw.githubusercontent.com/sboulema/BabyTracker/main/art/Login_thumb.png)](https://raw.githubusercontent.com/sboulema/BabyTracker/main/art/Login.png)
[![LoginRegisterReset](https://raw.githubusercontent.com/sboulema/BabyTracker/main/art/LoginRegisterReset_thumb.png)](https://raw.githubusercontent.com/sboulema/BabyTracker/main/art/LoginRegisterReset.png)
[![Profile](https://raw.githubusercontent.com/sboulema/BabyTracker/main/art/Profile_thumb.png)](https://raw.githubusercontent.com/sboulema/BabyTracker/main/art/Profile.png)

Creating an account is really easy and protects all the data about your baby.<br>
In your profile you can enable the memories email and share a baby with another user.

### Import / Select baby
[![Import](https://raw.githubusercontent.com/sboulema/BabyTracker/main/art/Import_thumb.png)](https://raw.githubusercontent.com/sboulema/BabyTracker/main/art/Import.png)
[![Load](https://raw.githubusercontent.com/sboulema/BabyTracker/main/art/LoadBaby_thumb.png)](https://raw.githubusercontent.com/sboulema/BabyTracker/main/art/LoadBaby.png)

Import a Data clone from the BabyTracker mobile app and select which baby you want to view.<br>
Importing a Data clone is handled by [Tus](https://tus.io/) so even if your clone has a large size due to photo's, importing will still succeed.

### Diary
[![Diary](https://raw.githubusercontent.com/sboulema/BabyTracker/main/art/Diary_thumb.png)](https://raw.githubusercontent.com/sboulema/BabyTracker/main/art/Diary.png)
[![DiaryCards](https://raw.githubusercontent.com/sboulema/BabyTracker/main/art/DiaryCards_thumb.png)](https://raw.githubusercontent.com/sboulema/BabyTracker/main/art/DiaryCards.png)
[![Memories](https://raw.githubusercontent.com/sboulema/BabyTracker/main/art/Memories_thumb.png)](https://raw.githubusercontent.com/sboulema/BabyTracker/main/art/Memories.png)
[![Gallery](https://raw.githubusercontent.com/sboulema/BabyTracker/main/art/Gallery_thumb.png)](https://raw.githubusercontent.com/sboulema/BabyTracker/main/art/Gallery.png)

View all events logged with the BabyTracker app on the big screen!<br>
See your memories, what happened a year ago? what happened 2 years ago?<br>
See a gallery of all the baby photo's

### Charts

[![Charts](https://raw.githubusercontent.com/sboulema/BabyTracker/main/art/Charts_thumb.png)](https://raw.githubusercontent.com/sboulema/BabyTracker/main/art/Charts.png)

BabyTracker shows you a number of different growth charts for your baby:
- Length/height
- Weight
- Head circumference
- BMI

Your babies data is plotted against the 3%, 50% and 97% percentile taken from the [WHO Child growth standards](https://www.who.int/tools/child-growth-standards/standards)


## Running
```
docker run -p 80:80 -e ... -e ... sboulema/babytracker
```

## Environment variables

| Variable					 | Description								|
|----------------------------|------------------------------------------|
| ASPNETCORE_ENVIRONMENT	 | .NET environment should be set to "Production" when using Docker, in order to used mapped volumes |
| AUTH0_CLIENTID			 | Auth0 clientid used for authentication |
| AUTH0_CLIENTSECRET		 | Auth0 client secret used for authentication |
| AUTH0_DOMAIN				 | Auth0 domain to use for authentication |
| AUTH0_MACHINE_CLIENTID	 | Auth0 machine to machine clientid used for user profile management |
| AUTH0_MACHINE_CLIENTSECRET | Auth0 machine to machine client secret used for user profile management |
| BASE_URL					 | Base url for links and images in the memories email |
| MEMORIES_CRON				 | Cron schedule on which to send the memories email |
| MEMORIES_FROM_EMAIL		 | Email address to use when sending memories email |
| MEMORIES_FROM_NAME		 | Email sender name to use when sending memories email |
| SENDGRID_API_KEY			 | SendGrid API key used to send the memories email |

## Volumes / Bind mounts

| Path on container | Description                         |
|-------------------|-------------------------------------|
| /data             | Data clones are stored at this path |

## Requirements
- Auth0 account
- Sendgrid account

## Building Dependencies
- .NET 9.0
- NPM

### Debugging
Cron to easily test the `MemoriesJob` by running it every minute:
`0 0/1 * 1/1 * ? *`

## Disclaimer
This website is not produced, endorsed, supported, or affiliated with nighp software.