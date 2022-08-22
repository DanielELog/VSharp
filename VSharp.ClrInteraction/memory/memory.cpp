#include "memory.h"
#include "stack.h"
#include <mutex>

using namespace vsharp;

ThreadID currentThreadNotConfigured() {
    throw std::logic_error("Current thread getter is not configured!");
}

std::function<ThreadID()> vsharp::currentThread(&currentThreadNotConfigured);

Storage vsharp::heap = Storage();

#ifdef _DEBUG
std::map<unsigned, const char*> vsharp::stringsPool;
int topStringIndex = 0;
#endif

ThreadID lastThreadID = 0;
Stack *currentStack = nullptr;

inline void switchContext() {
    ThreadID tid = currentThread();
    if (tid != lastThreadID) {
        lastThreadID = tid;
        Stack *&s = stacks[tid];
        if (!s) s = new Stack(heap);
        currentStack = s;
    }
}

Stack &vsharp::stack() {
    switchContext();
    return *currentStack;
}

StackFrame &vsharp::topFrame() {
    switchContext();
    return currentStack->topFrame();
}

void vsharp::validateStackEmptyness() {
#ifdef _DEBUG
    for (auto &kv : stacks) {
        if (!kv.second->isEmpty()) {
            FAIL_LOUD("Stack is not empty after program termination!");
        }
        if (!kv.second->opmemIsEmpty()) {
            FAIL_LOUD("Opmem is not empty after program termination!");
        }
    }
#endif
}

#ifdef _DEBUG
unsigned vsharp::allocateString(const char *s) {
    unsigned currentIndex = topStringIndex;
    // Place s into intern pool
    stringsPool[currentIndex] = s;
//    LOG(tout << "Allocated string '" << s << "' with index '" << currentIndex << "'");
    // Increment top index
    topStringIndex++;
    // Return string's index
    return currentIndex;
}
#endif

bool _mainLeft = false;

void vsharp::mainLeft() {
    _mainLeft = true;
}

bool vsharp::isMainLeft() {
    return _mainLeft;
}

bool instrumentationEnabled = true;

bool vsharp::instrumentingEnabled() {
    return instrumentationEnabled;
}

void vsharp::enabledInstrumentation() {
    assert(!instrumentationEnabled);
    instrumentationEnabled = true;
}

void vsharp::disableInstrumentation() {
    assert(instrumentationEnabled);
    instrumentationEnabled = false;
}

void vsharp::resolve(INT_PTR p, VirtualAddress &address) {
    heap.physToVirtAddress(p, address);
}
