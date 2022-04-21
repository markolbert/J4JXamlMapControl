# XAML Map Control

This is a fork of a fabulous XAML map control created by ClemensFischer. You can
[find their repository here](https://github.com/ClemensFischer/XAML-Map-Control).

I created this fork because the original project is structured around tiled map cloud services.
Such a service returns map imagery based on an (x, y, zoom) concept, where x and y are derived from latitude,
longitude and the particular type of projection (e.g., Mercator) you're using. I think :) -- I'm not an expert on
mapping servers.

While the tiled approach is quite common and easy to use there's one map source that doesn't offer that
capability, at least in Q2 2022: Google. Google also specifically prohibits  caching its map imagery, which is another conflict
with the architectural design of the ClemensFischer XAML map control.

So I decided to see if I could generalize the ClemensFischer control to enable the use of un-tiled map servers. Along
the way I updated and reorganized the code, added logging (via my J4JLogger library), etc. This repository is the
result.

To keep things simple I also restricted my fork to support only WinUI3, aka the Windows App SDK. That's because I'm trying to
migrate my Windows desktop programming away from WPF (and I never used UWP). 

Apologies if this irritates
those looking for an XAML map control to use within WPF and UWP projects. I encourage you to simply use the original
ClemensFischer control.
