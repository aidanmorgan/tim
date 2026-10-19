#include <mono/jit/jit.h>"
extern void *mono_aot_module_CuriousContraptions_Abstractions_info;
extern void *mono_aot_module_CuriousContraptions_Animation_info;
extern void *mono_aot_module_CuriousContraptions_Animation_Worker_info;
extern void *mono_aot_module_CuriousContraptions_FlatBuffers_info;
extern void *mono_aot_module_System_Collections_info;
extern void *mono_aot_module_System_Collections_Immutable_info;
extern void *mono_aot_module_System_Console_info;
extern void *mono_aot_module_System_Linq_info;
extern void *mono_aot_module_System_Memory_info;
extern void *mono_aot_module_System_Numerics_Vectors_info;
extern void *mono_aot_module_System_Private_CoreLib_info;
extern void *mono_aot_module_System_Runtime_info;
extern void *mono_aot_module_System_Runtime_InteropServices_JavaScript_info;
extern void *mono_aot_module_System_Runtime_Intrinsics_info;
extern void *mono_aot_module_System_Text_Encodings_Web_info;
extern void *mono_aot_module_System_Text_Json_info;
extern void *mono_aot_module_aot_instances_info;

void register_aot_modules (void);
void register_aot_modules (void)
{
    mono_aot_register_module (mono_aot_module_CuriousContraptions_Abstractions_info);
    mono_aot_register_module (mono_aot_module_CuriousContraptions_Animation_info);
    mono_aot_register_module (mono_aot_module_CuriousContraptions_Animation_Worker_info);
    mono_aot_register_module (mono_aot_module_CuriousContraptions_FlatBuffers_info);
    mono_aot_register_module (mono_aot_module_System_Collections_info);
    mono_aot_register_module (mono_aot_module_System_Collections_Immutable_info);
    mono_aot_register_module (mono_aot_module_System_Console_info);
    mono_aot_register_module (mono_aot_module_System_Linq_info);
    mono_aot_register_module (mono_aot_module_System_Memory_info);
    mono_aot_register_module (mono_aot_module_System_Numerics_Vectors_info);
    mono_aot_register_module (mono_aot_module_System_Private_CoreLib_info);
    mono_aot_register_module (mono_aot_module_System_Runtime_info);
    mono_aot_register_module (mono_aot_module_System_Runtime_InteropServices_JavaScript_info);
    mono_aot_register_module (mono_aot_module_System_Runtime_Intrinsics_info);
    mono_aot_register_module (mono_aot_module_System_Text_Encodings_Web_info);
    mono_aot_register_module (mono_aot_module_System_Text_Json_info);
    mono_aot_register_module (mono_aot_module_aot_instances_info);
}

#define EE_MODE_LLVMONLY 1