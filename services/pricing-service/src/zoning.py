"""
Découpage géographique en zones — implémentation geohash pure Python
(aucune dépendance externe requise), pour pouvoir calculer une zone à
partir de coordonnées (lat, lon), ou l'inverse.

À DISCUTER EN ÉQUIPE : Rzeigui/Mohamed Amine (matching) et Eya (carte)
utilisent aussi des coordonnées — le format de zone (geohash vs H3, et la
précision) doit être identique pour tout le monde (cf. Guide §11 et §19).
"""

_BASE32 = "0123456789bcdefghjkmnpqrstuvwxyz"


def encode_geohash(latitude: float, longitude: float, precision: int = 6) -> str:
    """Encode des coordonnées en identifiant de zone geohash."""
    lat_range = (-90.0, 90.0)
    lon_range = (-180.0, 180.0)
    geohash = []
    bits = 0
    bit_count = 0
    even_bit = True

    while len(geohash) < precision:
        if even_bit:
            mid = (lon_range[0] + lon_range[1]) / 2
            if longitude >= mid:
                bits = (bits << 1) | 1
                lon_range = (mid, lon_range[1])
            else:
                bits = bits << 1
                lon_range = (lon_range[0], mid)
        else:
            mid = (lat_range[0] + lat_range[1]) / 2
            if latitude >= mid:
                bits = (bits << 1) | 1
                lat_range = (mid, lat_range[1])
            else:
                bits = bits << 1
                lat_range = (lat_range[0], mid)

        even_bit = not even_bit
        bit_count += 1
        if bit_count == 5:
            geohash.append(_BASE32[bits])
            bits = 0
            bit_count = 0

    return "".join(geohash)


def zone_bounding_box(zone_id: str) -> dict:
    """Retourne la boîte englobante (lat/lon min et max) d'une zone geohash.

    Utile pour la documentation/debug, et pour que les autres modules
    (matching, carte) puissent vérifier qu'ils utilisent le même découpage.
    """
    lat_range = [-90.0, 90.0]
    lon_range = [-180.0, 180.0]
    even_bit = True

    for char in zone_id:
        idx = _BASE32.index(char)
        for shift in range(4, -1, -1):
            bit = (idx >> shift) & 1
            if even_bit:
                mid = (lon_range[0] + lon_range[1]) / 2
                if bit:
                    lon_range[0] = mid
                else:
                    lon_range[1] = mid
            else:
                mid = (lat_range[0] + lat_range[1]) / 2
                if bit:
                    lat_range[0] = mid
                else:
                    lat_range[1] = mid
            even_bit = not even_bit

    return {
        "lat_min": lat_range[0],
        "lat_max": lat_range[1],
        "lon_min": lon_range[0],
        "lon_max": lon_range[1],
    }
