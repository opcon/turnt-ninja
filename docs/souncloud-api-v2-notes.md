# Explore API

**Get explore categories**

```
https://api-v2.soundcloud.com/explore/categories?limit=10&offset=0&linked_partitioning=1&client_id=ID_HERE
```

Example
```
https://api-v2.soundcloud.com/explore/categories?limit=10&offset=0&linked_partitioning=1&client_id=02gUJC0hH2ct1EGOcYXQIzRFU91c72Ea&app_version=d8c55ad
```

**Get tracks from explore category**
```
https://api-v2.soundcloud.com/explore/CATEGORY_NAME_HERE?tag=out-of-experiment&limit=10&offset=0&linked_partitioning=1&client_id=ID_HERE
```

Example
```
https://api-v2.soundcloud.com/explore/Hip+Hop+%26+Rap?tag=out-of-experiment&limit=10&client_id=ID_HERE
```

# New Search API

**Search Everything**
```
https://api-v2.soundcloud.com/search?q=antidote&facet=model&user_id=698189-13257-213778-325874&limit=10&offset=0&linked_partitioning=1&client_id=02gUJC0hH2ct1EGOcYXQIzRFU91c72Ea&app_version=d8c55ad
```

**Search only tracks**
```
https://api-v2.soundcloud.com/search/tracks?q=antidote&facet=genre&user_id=698189-13257-213778-325874&limit=10&offset=0&linked_partitioning=1&client_id=02gUJC0hH2ct1EGOcYXQIzRFU91c72Ea&app_version=d8c55ad
```

# New Charts API

**Get All-Music Chart**
```
https://api-v2.soundcloud.com/charts?kind=top&genre=soundcloud%3Agenres%3Aall-music&client_id=02gUJC0hH2ct1EGOcYQXIzRFU91c72Ea&limit=20&offset=0&linked_partitioning=1&app_version=1461312517
```