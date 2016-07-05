# Mono with CoreGC

I started working on adapting the CoreCLR GC to Mono.  I didn't get very
far, but I managed to change the GC to use an object layout that's closer
to Mono's:

* GC descriptors are stored separately from the VTable.  CoreGC expects
  descriptors at negative offsets from the VTable, but that's where we store
  the IMT, which is something we probably don't want to change.

* The lock word is where Mono expects it.  CoreGC has the lock word directly
  before the VTable, i.e. at negative one word.

## Building

I figured it would be easier to bootstrap on Windows, but since I know
nothing about Visual Studio my project is a huge hack, and in addition to
that, Windows has a 256 character total path length limit, with is a problem
with Mono.  

Clone the [Mono CoreGC branch](https://github.com/schani/mono/tree/feature-coregc) into `C:\mono`, and the [CoreGC Mono branch](https://github.com/schani/coreclr/tree/mono) into `C:\Users\Mark Probst\Source\Repos\coreclr`.  Mono should then build in VS15 and link with CoreGC.  Right now all it does is run the CoreGC sample app before starting Mono proper.

## GSoC Effort

I learned too late that a GSoC student from last year [had already put some
work in](https://github.com/Spo1ler/mono/commits/master
).  I didn't have much time to look at it, but they seem to have
integrated with Mono already.  The approach they seem to take is to use
SGen's GC descriptor mechanics, and to do more invasive changes to adapt
Mono's object layout, like removing the CoreCLR managed object C++ class, but
I'm not sure why that would be necessary.  It might be detrimental to a
future integration effort.


