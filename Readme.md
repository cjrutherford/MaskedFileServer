# Masked File Server

This Masked File Server is intended to make it simple to serve files in a pseudo hidden way. The actual file names are masked in the url via a GUID (*Globally Unique Identifier*). It works by monitoring a folder for file changes. It also maintains a retention schedule so that files over a certain age will be automatically removed from availability.

## .Net Environment Variables:

### File_Path
This is the single location that MFS will monitor and maintiain automatically. This should always be a string, and there are no special requirements for escape characters in the file path. **Simply copy and paste the directory, and MFS will take care of the rest.** The default for this is **TBD**.

### Expiration_Term
This is the number of days that files should be available prior to automatic deletion. The default is 90 days.

### Delete_On_Expiry
This is simply a Boolean that determines if files should be deleted on expiration. 
