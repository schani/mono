# Mono with CoreGC

I started working on adapting the CoreCLR GC to Mono.  I didn't get very
far, but I managed to change the GC to use an object layout that's closer
to Mono's:

* GC descriptors are stored separately from the VTable.  CoreGC expects
  descriptors at negative offsets from the VTable, but that's where we store
  the IMT, which is something we probably don't want to change.

* The lock word is where Mono expects it.  CoreGC has the lock word directly
  before the VTable, i.e. at negative one word.

With these changes, the GC sample app runs, and I managed to link that
with Mono, but there is no integration between the two yet.

## Building

I figured it would be easier to bootstrap on Windows, but since I know
nothing about Visual Studio my project is a huge hack, and in addition to
that, Windows has a 256 character total path length limit, with is a problem
with Mono.

Clone the
[Mono CoreGC branch](https://github.com/schani/mono/tree/feature-coregc)
into `C:\mono`, and the
[CoreGC Mono branch](https://github.com/schani/coreclr/tree/mono) into
`C:\Users\Mark Probst\Source\Repos\coreclr`.  Mono should then build
in VS15 and link with CoreGC.  Right now all it does is run the CoreGC
sample app before starting Mono proper.

## GSoC Effort

I learned too late that a GSoC student from last year [had already put some
work in](https://github.com/Spo1ler/mono/commits/master
).  I didn't have much time to look at it, but they seem to have
integrated with Mono already.  The approach they seem to take is to use
SGen's GC descriptor mechanics, and to do more invasive changes to adapt
Mono's object layout, like removing the CoreCLR managed object C++ class, but
I'm not sure why that would be necessary.  It might be detrimental to a
future integration effort.

## What to do next

If we continue with the GSoC effort, then I don't know - I haven't had
time to look into it.

My thing needs to be integrated with Mono.  Kumpera did work on the
scaffold, which the GSoC thing also uses, so that would be a start.

Allocating GC descriptors would have to be implemented.  For a start
they could just come out of the domain mempools, and the VTables would
point there.

Implement a basic write barrier.  The Dijkstra branch modifies all the
write barriers to reduce to the one-reference, generic one, so that
might be useful to get started.

I also think the GC wants a few bits out of the lock word, but we'd
have to change that to use the lower VTable bits instead, like SGen
uses.

Then we'd have to tell the GC to conservatively scan the stacks (Maoni
will know how to do this), and to register the roots.  At that point
very basic tests should work.

Figure out how to do domain unloading.

Finalizers and weak links would be next, also ephemerons.

At that point basic integration would be feature-complete.  Test
performance.  Check benchmarks such as lcscbench, which required
cementing for SGen - does CoreGC handle those gracefully?

How to do concurrent GC?  If we use mark bits in the VTable, that
won't work directly.
