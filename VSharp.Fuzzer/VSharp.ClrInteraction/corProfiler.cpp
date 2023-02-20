#include "corProfiler.h"
#ifdef UNIX
#include "profiler_unix.h"
#endif
#ifdef WIN32
#include "profiler_win.h"
#endif
#include "logging.h"
#include "instrumenter.h"
#include "memory/memory.h"

#define UNUSED(x) (void)x

using namespace vsharp;


CorProfiler::CorProfiler() : refCount(0), corProfilerInfo(nullptr), instrumenter(nullptr)
{
}

CorProfiler::~CorProfiler()
{
    if (this->corProfilerInfo != nullptr)
    {
        this->corProfilerInfo->Release();
        this->corProfilerInfo = nullptr;
        delete instrumenter;
    }
}

HRESULT STDMETHODCALLTYPE CorProfiler::Initialize(IUnknown *pICorProfilerInfoUnk)
{
    HRESULT queryInterfaceResult = pICorProfilerInfoUnk->QueryInterface(__uuidof(ICorProfilerInfo8), reinterpret_cast<void **>(&this->corProfilerInfo));

    if (FAILED(queryInterfaceResult))
    {
        return E_FAIL;
    }

    DWORD eventMask =
        COR_PRF_MONITOR_JIT_COMPILATION |
        COR_PRF_DISABLE_ALL_NGEN_IMAGES |
//        COR_PRF_DISABLE_OPTIMIZATIONS |
//        COR_PRF_MONITOR_CACHE_SEARCHES |
        COR_PRF_MONITOR_EXCEPTIONS |
        COR_PRF_MONITOR_CLR_EXCEPTIONS |
        COR_PRF_DISABLE_TRANSPARENCY_CHECKS_UNDER_FULL_TRUST | /* helps the case where this profiler is used on Full CLR */
        COR_PRF_DISABLE_INLINING |
//        COR_PRF_MONITOR_GC |
//        COR_PRF_ENABLE_OBJECT_ALLOCATED |
//        COR_PRF_MONITOR_OBJECT_ALLOCATED |
        COR_PRF_ENABLE_REJIT;

    // TODO: place IfFailRet here, log fails!
    auto hr = this->corProfilerInfo->SetEventMask(eventMask);

#ifdef _LOGGING
    open_log();
#endif

    auto currentThreadGetter = [=]() {
        ThreadID result;
        HRESULT hr = corProfilerInfo->GetCurrentThreadID(&result);
        if (hr != S_OK) {
            LOG_ERROR(tout << "getting current thread failed with HRESULT = " << std::hex << hr);
        }
        return result;
    };
    currentThread = currentThreadGetter;

    instrumenter = new Instrumenter(*corProfilerInfo);

    return S_OK;
}

HRESULT STDMETHODCALLTYPE CorProfiler::Shutdown()
{
#ifdef _LOGGING
    close_log();
#endif

    if (this->corProfilerInfo != nullptr)
    {
        this->corProfilerInfo->Release();
        this->corProfilerInfo = nullptr;
        //delete instrumenter;
    }

    return S_OK;
}

HRESULT STDMETHODCALLTYPE CorProfiler::AppDomainCreationStarted(AppDomainID appDomainId)
{
    UNUSED(appDomainId);
    return S_OK;
}

HRESULT STDMETHODCALLTYPE CorProfiler::AppDomainCreationFinished(AppDomainID appDomainId, HRESULT hrStatus)
{
    UNUSED(appDomainId);
    UNUSED(hrStatus);
    return S_OK;
}

HRESULT STDMETHODCALLTYPE CorProfiler::AppDomainShutdownStarted(AppDomainID appDomainId)
{
    UNUSED(appDomainId);
    return S_OK;
}

HRESULT STDMETHODCALLTYPE CorProfiler::AppDomainShutdownFinished(AppDomainID appDomainId, HRESULT hrStatus)
{
    UNUSED(appDomainId);
    UNUSED(hrStatus);
    return S_OK;
}

HRESULT STDMETHODCALLTYPE CorProfiler::AssemblyLoadStarted(AssemblyID assemblyId)
{
    UNUSED(assemblyId);
    return S_OK;
}

HRESULT STDMETHODCALLTYPE CorProfiler::AssemblyLoadFinished(AssemblyID assemblyId, HRESULT hrStatus)
{
    UNUSED(assemblyId);
    UNUSED(hrStatus);
    return S_OK;
}

HRESULT STDMETHODCALLTYPE CorProfiler::AssemblyUnloadStarted(AssemblyID assemblyId)
{
    UNUSED(assemblyId);
    return S_OK;
}

HRESULT STDMETHODCALLTYPE CorProfiler::AssemblyUnloadFinished(AssemblyID assemblyId, HRESULT hrStatus)
{
    UNUSED(assemblyId);
    UNUSED(hrStatus);
    return S_OK;
}

HRESULT STDMETHODCALLTYPE CorProfiler::ModuleLoadStarted(ModuleID moduleId)
{
    UNUSED(moduleId);
    return S_OK;
}

HRESULT STDMETHODCALLTYPE CorProfiler::ModuleLoadFinished(ModuleID moduleId, HRESULT hrStatus)
{
    UNUSED(moduleId);
    UNUSED(hrStatus);
    return S_OK;
}

HRESULT STDMETHODCALLTYPE CorProfiler::ModuleUnloadStarted(ModuleID moduleId)
{
    UNUSED(moduleId);
    return S_OK;
}

HRESULT STDMETHODCALLTYPE CorProfiler::ModuleUnloadFinished(ModuleID moduleId, HRESULT hrStatus)
{
    UNUSED(moduleId);
    UNUSED(hrStatus);
    return S_OK;
}

HRESULT STDMETHODCALLTYPE CorProfiler::ModuleAttachedToAssembly(ModuleID moduleId, AssemblyID assemblyId)
{
    UNUSED(moduleId);
    UNUSED(assemblyId);
    return S_OK;
}

HRESULT STDMETHODCALLTYPE CorProfiler::ClassLoadStarted(ClassID classId)
{
    UNUSED(classId);
    return S_OK;
}

HRESULT STDMETHODCALLTYPE CorProfiler::ClassLoadFinished(ClassID classId, HRESULT hrStatus)
{
    UNUSED(classId);
    UNUSED(hrStatus);
    return S_OK;
}

HRESULT STDMETHODCALLTYPE CorProfiler::ClassUnloadStarted(ClassID classId)
{
    UNUSED(classId);
    return S_OK;
}

HRESULT STDMETHODCALLTYPE CorProfiler::ClassUnloadFinished(ClassID classId, HRESULT hrStatus)
{
    UNUSED(classId);
    UNUSED(hrStatus);
    return S_OK;
}

HRESULT STDMETHODCALLTYPE CorProfiler::FunctionUnloadStarted(FunctionID functionId)
{
    UNUSED(functionId);
    return S_OK;
}

#include<iostream>
#include <vector>

bool jitInProcess = false;

HRESULT STDMETHODCALLTYPE CorProfiler::JITCompilationStarted(FunctionID functionId, BOOL fIsSafeToBlock)
{
    getLock();
    LOG(tout << "JITCompilationStarted, threadID = " << currentThread() << " funcId = " << functionId << std::endl);
    UNUSED(fIsSafeToBlock);
    auto instrument = new Instrumenter(*corProfilerInfo);
    HRESULT hr = instrument->instrument(functionId, false);
    freeLock();
    return hr;
}

HRESULT STDMETHODCALLTYPE CorProfiler::JITCompilationFinished(FunctionID functionId, HRESULT hrStatus, BOOL fIsSafeToBlock)
{
//    std::cout << __FUNCTION__ << std::endl;
    UNUSED(functionId);
    UNUSED(hrStatus);
    UNUSED(fIsSafeToBlock);
    return S_OK;
}

HRESULT STDMETHODCALLTYPE CorProfiler::JITCachedFunctionSearchStarted(FunctionID functionId, BOOL *pbUseCachedFunction)
{
    LOG(tout << __FUNCTION__ << std::endl);
    UNUSED(functionId);
    UNUSED(pbUseCachedFunction);
    return S_OK;
}

HRESULT STDMETHODCALLTYPE CorProfiler::JITCachedFunctionSearchFinished(FunctionID functionId, COR_PRF_JIT_CACHE result)
{
    LOG(tout << __FUNCTION__ << std::endl);
    UNUSED(functionId);
    UNUSED(result);
    return S_OK;
}

HRESULT STDMETHODCALLTYPE CorProfiler::JITFunctionPitched(FunctionID functionId)
{
    LOG(tout << __FUNCTION__ << std::endl);
    UNUSED(functionId);
    return S_OK;
}

HRESULT STDMETHODCALLTYPE CorProfiler::JITInlining(FunctionID callerId, FunctionID calleeId, BOOL *pfShouldInline)
{
    LOG(tout << __FUNCTION__ << std::endl);
    UNUSED(callerId);
    UNUSED(calleeId);
    UNUSED(pfShouldInline);
    return S_OK;
}

HRESULT STDMETHODCALLTYPE CorProfiler::ThreadCreated(ThreadID threadId)
{
    UNUSED(threadId);
    return S_OK;
}

HRESULT STDMETHODCALLTYPE CorProfiler::ThreadDestroyed(ThreadID threadId)
{
    UNUSED(threadId);
    return S_OK;
}

HRESULT STDMETHODCALLTYPE CorProfiler::ThreadAssignedToOSThread(ThreadID managedThreadId, DWORD osThreadId)
{
    UNUSED(managedThreadId);
    UNUSED(osThreadId);
    return S_OK;
}

HRESULT STDMETHODCALLTYPE CorProfiler::RemotingClientInvocationStarted()
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE CorProfiler::RemotingClientSendingMessage(GUID *pCookie, BOOL fIsAsync)
{
    UNUSED(pCookie);
    UNUSED(fIsAsync);
    return S_OK;
}

HRESULT STDMETHODCALLTYPE CorProfiler::RemotingClientReceivingReply(GUID *pCookie, BOOL fIsAsync)
{
    UNUSED(pCookie);
    UNUSED(fIsAsync);
    return S_OK;
}

HRESULT STDMETHODCALLTYPE CorProfiler::RemotingClientInvocationFinished()
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE CorProfiler::RemotingServerReceivingMessage(GUID *pCookie, BOOL fIsAsync)
{
    UNUSED(pCookie);
    UNUSED(fIsAsync);
    return S_OK;
}

HRESULT STDMETHODCALLTYPE CorProfiler::RemotingServerInvocationStarted()
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE CorProfiler::RemotingServerInvocationReturned()
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE CorProfiler::RemotingServerSendingReply(GUID *pCookie, BOOL fIsAsync)
{
    UNUSED(pCookie);
    UNUSED(fIsAsync);
    return S_OK;
}

HRESULT STDMETHODCALLTYPE CorProfiler::UnmanagedToManagedTransition(FunctionID functionId, COR_PRF_TRANSITION_REASON reason)
{
    UNUSED(functionId);
    UNUSED(reason);
    return S_OK;
}

HRESULT STDMETHODCALLTYPE CorProfiler::ManagedToUnmanagedTransition(FunctionID functionId, COR_PRF_TRANSITION_REASON reason)
{
    UNUSED(functionId);
    UNUSED(reason);
    return S_OK;
}

HRESULT STDMETHODCALLTYPE CorProfiler::RuntimeSuspendStarted(COR_PRF_SUSPEND_REASON suspendReason)
{
    UNUSED(suspendReason);
    return S_OK;
}

HRESULT STDMETHODCALLTYPE CorProfiler::RuntimeSuspendFinished()
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE CorProfiler::RuntimeSuspendAborted()
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE CorProfiler::RuntimeResumeStarted()
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE CorProfiler::RuntimeResumeFinished()
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE CorProfiler::RuntimeThreadSuspended(ThreadID threadId)
{
    UNUSED(threadId);
    return S_OK;
}

HRESULT STDMETHODCALLTYPE CorProfiler::RuntimeThreadResumed(ThreadID threadId)
{
    UNUSED(threadId);
    return S_OK;
}

HRESULT STDMETHODCALLTYPE CorProfiler::MovedReferences(ULONG cMovedObjectIDRanges, ObjectID oldObjectIDRangeStart[], ObjectID newObjectIDRangeStart[], ULONG cObjectIDRangeLength[])
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE CorProfiler::ObjectAllocated(ObjectID objectId, ClassID classId)
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE CorProfiler::ObjectsAllocatedByClass(ULONG cClassCount, ClassID classIds[], ULONG cObjects[])
{
    UNUSED(cClassCount);
    UNUSED(classIds);
    UNUSED(cObjects);
    return S_OK;
}

HRESULT STDMETHODCALLTYPE CorProfiler::ObjectReferences(ObjectID objectId, ClassID classId, ULONG cObjectRefs, ObjectID objectRefIds[])
{
    UNUSED(objectId);
    UNUSED(classId);
    UNUSED(cObjectRefs);
    UNUSED(objectRefIds);
    return S_OK;
}

HRESULT STDMETHODCALLTYPE CorProfiler::RootReferences(ULONG cRootRefs, ObjectID rootRefIds[])
{
    UNUSED(cRootRefs);
    UNUSED(rootRefIds);
    return S_OK;
}

HRESULT STDMETHODCALLTYPE CorProfiler::ExceptionThrown(ObjectID thrownObjectId)
{
    LOG(tout << "EXCEPTION THROWN!" << std::endl);
    UNUSED(thrownObjectId);
    return S_OK;
}

HRESULT STDMETHODCALLTYPE CorProfiler::ExceptionSearchFunctionEnter(FunctionID functionId)
{
    LOG(tout << "EXCEPTION Search function enter" << std::endl);
    UNUSED(functionId);
    return S_OK;
}

HRESULT STDMETHODCALLTYPE CorProfiler::ExceptionSearchFunctionLeave()
{
    LOG(tout << "EXCEPTION Search function leave" << std::endl);
    // TODO: use for filter support
    return S_OK;
}

HRESULT STDMETHODCALLTYPE CorProfiler::ExceptionSearchFilterEnter(FunctionID functionId)
{
    LOG(tout << "EXCEPTION Search filter enter" << std::endl);

    UNUSED(functionId);
    return S_OK;
}

HRESULT STDMETHODCALLTYPE CorProfiler::ExceptionSearchFilterLeave()
{
    LOG(tout << "EXCEPTION Search filter leave" << std::endl);
    if (isMainEntered()) {
        // TODO: support filter
        FAIL_LOUD("Filter is not supported");
    }
    return S_OK;
}

HRESULT STDMETHODCALLTYPE CorProfiler::ExceptionSearchCatcherFound(FunctionID functionId)
{
    LOG(tout << "EXCEPTION Search catcher found" << std::endl);
    UNUSED(functionId);
    return S_OK;
}

HRESULT STDMETHODCALLTYPE CorProfiler::ExceptionOSHandlerEnter(UINT_PTR ptr)
{
    LOG(tout << "EXCEPTION OS HANDLER ENTER!" << std::endl);
    UNUSED(ptr);
    return S_OK;
}

HRESULT STDMETHODCALLTYPE CorProfiler::ExceptionOSHandlerLeave(UINT_PTR ptr)
{
    LOG(tout << "EXCEPTION OS HANDLER LEAVE!" << std::endl);
    UNUSED(ptr);
    return S_OK;
}

HRESULT STDMETHODCALLTYPE CorProfiler::ExceptionUnwindFunctionEnter(FunctionID functionId)
{
    LOG(tout << "EXCEPTION UNWIND FUNCTION ENTER!" << std::endl);
    UNUSED(functionId);
    return S_OK;
}

HRESULT STDMETHODCALLTYPE CorProfiler::ExceptionUnwindFunctionLeave()
{
    LOG(tout << "EXCEPTION UNWIND FUNCTION LEAVE!" << std::endl);
    if (isMainEntered()) {
        Stack &s = stack();
        if (s.framesCount() == 1) {
            // NOTE: sending command to end SILI execution
        }
        // NOTE: popping stack frame
        s.popFrame();
    }
    return S_OK;
}

HRESULT STDMETHODCALLTYPE CorProfiler::ExceptionUnwindFinallyEnter(FunctionID functionId)
{
    LOG(tout << "EXCEPTION UNWIND FINALLY ENTER!" << std::endl);
    UNUSED(functionId);
    return S_OK;
}

HRESULT STDMETHODCALLTYPE CorProfiler::ExceptionUnwindFinallyLeave()
{
    LOG(tout << "EXCEPTION UNWIND FINALLY LEAVE!" << std::endl);
    return S_OK;
}

HRESULT STDMETHODCALLTYPE CorProfiler::ExceptionCatcherEnter(FunctionID functionId, ObjectID objectId)
{
    LOG(tout << "EXCEPTION CATCHER ENTER!" << std::endl);
    UNUSED(functionId);
    UNUSED(objectId);
    return S_OK;
}

HRESULT STDMETHODCALLTYPE CorProfiler::ExceptionCatcherLeave()
{
    LOG(tout << "EXCEPTION CATCHER Leave!" << std::endl);
    return S_OK;
}

HRESULT STDMETHODCALLTYPE CorProfiler::COMClassicVTableCreated(ClassID wrappedClassId, REFGUID implementedIID, void *pVTable, ULONG cSlots)
{
    UNUSED(wrappedClassId);
    UNUSED(implementedIID);
    UNUSED(pVTable);
    UNUSED(cSlots);
    return S_OK;
}

HRESULT STDMETHODCALLTYPE CorProfiler::COMClassicVTableDestroyed(ClassID wrappedClassId, REFGUID implementedIID, void *pVTable)
{
    UNUSED(wrappedClassId);
    UNUSED(implementedIID);
    UNUSED(pVTable);
    return S_OK;
}

HRESULT STDMETHODCALLTYPE CorProfiler::ExceptionCLRCatcherFound()
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE CorProfiler::ExceptionCLRCatcherExecute()
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE CorProfiler::ThreadNameChanged(ThreadID threadId, ULONG cchName, WCHAR name[])
{
    UNUSED(threadId);
    UNUSED(cchName);
    UNUSED(name);
    return S_OK;
}

HRESULT STDMETHODCALLTYPE CorProfiler::GarbageCollectionStarted(int cGenerations, BOOL generationCollected[], COR_PRF_GC_REASON reason)
{
    UNUSED(cGenerations);
    UNUSED(generationCollected);
    UNUSED(reason);
    return S_OK;
}

HRESULT STDMETHODCALLTYPE CorProfiler::SurvivingReferences(ULONG cSurvivingObjectIDRanges, ObjectID objectIDRangeStart[], ULONG cObjectIDRangeLength[])
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE CorProfiler::GarbageCollectionFinished()
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE CorProfiler::FinalizeableObjectQueued(DWORD finalizerFlags, ObjectID objectId)
{
    UNUSED(finalizerFlags);
    UNUSED(objectId);
    return S_OK;
}

HRESULT STDMETHODCALLTYPE CorProfiler::RootReferences2(ULONG cRootRefs, ObjectID rootRefIds[], COR_PRF_GC_ROOT_KIND rootKinds[], COR_PRF_GC_ROOT_FLAGS rootFlags[], UINT_PTR rootIds[])
{
    UNUSED(cRootRefs);
    UNUSED(rootRefIds);
    UNUSED(rootKinds);
    UNUSED(rootFlags);
    UNUSED(rootIds);
    return S_OK;
}

HRESULT STDMETHODCALLTYPE CorProfiler::HandleCreated(GCHandleID handleId, ObjectID initialObjectId)
{
    UNUSED(handleId);
    UNUSED(initialObjectId);
    return S_OK;
}

HRESULT STDMETHODCALLTYPE CorProfiler::HandleDestroyed(GCHandleID handleId)
{
    UNUSED(handleId);
    return S_OK;
}

HRESULT STDMETHODCALLTYPE CorProfiler::InitializeForAttach(IUnknown *pCorProfilerInfoUnk, void *pvClientData, UINT cbClientData)
{
    UNUSED(pCorProfilerInfoUnk);
    UNUSED(pvClientData);
    UNUSED(cbClientData);
    return S_OK;
}

HRESULT STDMETHODCALLTYPE CorProfiler::ProfilerAttachComplete()
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE CorProfiler::ProfilerDetachSucceeded()
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE CorProfiler::ReJITCompilationStarted(FunctionID functionId, ReJITID rejitId, BOOL fIsSafeToBlock)
{
    UNUSED(functionId);
    UNUSED(rejitId);
    UNUSED(fIsSafeToBlock);
    HRESULT hr = instrumenter->reInstrument(functionId);
    return hr;
}

HRESULT STDMETHODCALLTYPE CorProfiler::GetReJITParameters(ModuleID moduleId, mdMethodDef methodId, ICorProfilerFunctionControl *pFunctionControl)
{
    UNUSED(moduleId);
    UNUSED(methodId);
    UNUSED(pFunctionControl);
    return S_OK;
}

HRESULT STDMETHODCALLTYPE CorProfiler::ReJITCompilationFinished(FunctionID functionId, ReJITID rejitId, HRESULT hrStatus, BOOL fIsSafeToBlock)
{
    UNUSED(functionId);
    UNUSED(rejitId);
    UNUSED(hrStatus);
    UNUSED(fIsSafeToBlock);
    return S_OK;
}

HRESULT STDMETHODCALLTYPE CorProfiler::ReJITError(ModuleID moduleId, mdMethodDef methodId, FunctionID functionId, HRESULT hrStatus)
{
    UNUSED(moduleId);
    UNUSED(methodId);
    UNUSED(functionId);
    UNUSED(hrStatus);
    return S_OK;
}

HRESULT STDMETHODCALLTYPE CorProfiler::MovedReferences2(ULONG cMovedObjectIDRanges, ObjectID oldObjectIDRangeStart[], ObjectID newObjectIDRangeStart[], SIZE_T cObjectIDRangeLength[])
{
    UNUSED(cMovedObjectIDRanges);
    UNUSED(oldObjectIDRangeStart);
    UNUSED(newObjectIDRangeStart);
    UNUSED(cObjectIDRangeLength);
    return S_OK;
}

HRESULT STDMETHODCALLTYPE CorProfiler::SurvivingReferences2(ULONG cSurvivingObjectIDRanges, ObjectID objectIDRangeStart[], SIZE_T cObjectIDRangeLength[])
{
    UNUSED(cSurvivingObjectIDRanges);
    UNUSED(objectIDRangeStart);
    UNUSED(cObjectIDRangeLength);
    return S_OK;
}

HRESULT STDMETHODCALLTYPE CorProfiler::ConditionalWeakTableElementReferences(ULONG cRootRefs, ObjectID keyRefIds[], ObjectID valueRefIds[], GCHandleID rootIds[])
{
    UNUSED(cRootRefs);
    UNUSED(keyRefIds);
    UNUSED(valueRefIds);
    UNUSED(rootIds);
    return S_OK;
}

HRESULT STDMETHODCALLTYPE CorProfiler::GetAssemblyReferences(const WCHAR *wszAssemblyPath, ICorProfilerAssemblyReferenceProvider *pAsmRefProvider)
{
    UNUSED(wszAssemblyPath);
    UNUSED(pAsmRefProvider);
    return S_OK;
}

HRESULT STDMETHODCALLTYPE CorProfiler::ModuleInMemorySymbolsUpdated(ModuleID moduleId)
{
    UNUSED(moduleId);
    return S_OK;
}

HRESULT STDMETHODCALLTYPE CorProfiler::DynamicMethodJITCompilationStarted(FunctionID functionId, BOOL fIsSafeToBlock, LPCBYTE ilHeader, ULONG cbILHeader)
{
    // TODO: this should be instrumented as well!!
    ClassID classId;
    ModuleID moduleId;
    mdToken token;
    HRESULT hr = corProfilerInfo->GetFunctionInfo(functionId, &classId, &moduleId, &token);
    tout << "function info: success= " << SUCCEEDED(hr) << ", classId=" << classId << ", moduleId=" << moduleId << ", token=" << token << std::endl;
    return S_OK;
}

HRESULT STDMETHODCALLTYPE CorProfiler::DynamicMethodJITCompilationFinished(FunctionID functionId, HRESULT hrStatus, BOOL fIsSafeToBlock)
{
//    printf("Dynamic Function JIT Compilation Finished. %" UINT_PTR_FORMAT "\r\n", (UINT64)functionId);
    return S_OK;
}
