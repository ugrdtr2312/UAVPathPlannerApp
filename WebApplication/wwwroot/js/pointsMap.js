window.initMap = function () {
    // Custom map style to hide labels
    var mapStyle = [
        {
            "elementType": "labels",
            "stylers": [
                { "visibility": "off" }
            ]
        },
        {
            "featureType": "administrative",
            "elementType": "geometry",
            "stylers": [
                { "visibility": "off" }
            ]
        },
        {
            "featureType": "poi",
            "elementType": "labels",
            "stylers": [
                { "visibility": "off" }
            ]
        },
        {
            "featureType": "road",
            "elementType": "labels.icon",
            "stylers": [
                { "visibility": "off" }
            ]
        },
        {
            "featureType": "transit",
            "elementType": "labels.icon",
            "stylers": [
                { "visibility": "off" }
            ]
        }
    ];

    window.map = new google.maps.Map(document.getElementById('map'), {
        center: { lat: 50.4501, lng: 30.5234 }, // Centered on Kyiv
        zoom: 12, // Adjusted zoom level for better visibility of points in Kyiv
        styles: mapStyle
    });
    window.markers = [];
};

window.updateMarkers = function (points) {
    // Clear existing markers
    window.markers.forEach(marker => marker.setMap(null));
    window.markers = [];

    // Add new markers
    points.forEach(point => {
        var latLng = new google.maps.LatLng(parseFloat(point.Latitude), parseFloat(point.Longitude));
        var marker = new google.maps.Marker({
            position: latLng,
            map: window.map,
            icon: {
                path: google.maps.SymbolPath.CIRCLE,
                scale: 6,
                fillColor: 'blue',
                fillOpacity: 1,
                strokeWeight: 0
            }
        });
        window.markers.push(marker);
    });

    if (window.markers.length > 0) {
        var bounds = new google.maps.LatLngBounds();
        window.markers.forEach(marker => bounds.extend(marker.getPosition()));
        window.map.fitBounds(bounds);
    }
};