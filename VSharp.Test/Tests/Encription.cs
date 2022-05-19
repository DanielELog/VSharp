using System;
using VSharp.Test;
using DotNetSix;
using BCrypt.Net;
using BCrypt;
namespace IntegrationTests;

[TestSvmFixture]
public static class Encription
{
    static readonly string[,] _testVectors = {
        { "",                                   "$2a$06$DCq7YPn5Rq63x1Lad4cll.",    "$2a$06$DCq7YPn5Rq63x1Lad4cll.TV4S6ytwfsfvkgY8jIucDrjc8deX1s." },
        { "",                                   "$2a$08$HqWuK6/Ng6sg9gQzbLrgb.",    "$2a$08$HqWuK6/Ng6sg9gQzbLrgb.Tl.ZHfXLhvt/SgVyWhQqgqcZ7ZuUtye" },
        { "",                                   "$2a$10$k1wbIrmNyFAPwPVPSVa/ze",    "$2a$10$k1wbIrmNyFAPwPVPSVa/zecw2BCEnBwVS2GbrmgzxFUOqW9dk4TCW" },
        { "",                                   "$2a$12$k42ZFHFWqBp3vWli.nIn8u",    "$2a$12$k42ZFHFWqBp3vWli.nIn8uYyIkbvYRvodzbfbK18SSsY.CsIQPlxO" },
        { "a",                                  "$2a$06$m0CrhHm10qJ3lXRY.5zDGO",    "$2a$06$m0CrhHm10qJ3lXRY.5zDGO3rS2KdeeWLuGmsfGlMfOxih58VYVfxe" },
        { "a",                                  "$2a$08$cfcvVd2aQ8CMvoMpP2EBfe",    "$2a$08$cfcvVd2aQ8CMvoMpP2EBfeodLEkkFJ9umNEfPD18.hUF62qqlC/V." },
        { "a",                                  "$2a$10$k87L/MF28Q673VKh8/cPi.",    "$2a$10$k87L/MF28Q673VKh8/cPi.SUl7MU/rWuSiIDDFayrKk/1tBsSQu4u" },
        { "a",                                  "$2a$12$8NJH3LsPrANStV6XtBakCe",    "$2a$12$8NJH3LsPrANStV6XtBakCez0cKHXVxmvxIlcz785vxAIZrihHZpeS" },
        { "abc",                                "$2a$06$If6bvum7DFjUnE9p2uDeDu",    "$2a$06$If6bvum7DFjUnE9p2uDeDu0YHzrHM6tf.iqN8.yx.jNN1ILEf7h0i" },
        { "abc",                                "$2a$08$Ro0CUfOqk6cXEKf3dyaM7O",    "$2a$08$Ro0CUfOqk6cXEKf3dyaM7OhSCvnwM9s4wIX9JeLapehKK5YdLxKcm" },
        { "abc",                                "$2a$10$WvvTPHKwdBJ3uk0Z37EMR.",    "$2a$10$WvvTPHKwdBJ3uk0Z37EMR.hLA2W6N9AEBhEgrAOljy2Ae5MtaSIUi" },
        { "abc",                                "$2a$12$EXRkfkdmXn2gzds2SSitu.",    "$2a$12$EXRkfkdmXn2gzds2SSitu.MW9.gAVqa9eLS1//RYtYCmB1eLHg.9q" },
        { "abcdefghijklmnopqrstuvwxyz",         "$2a$06$.rCVZVOThsIa97pEDOxvGu",    "$2a$06$.rCVZVOThsIa97pEDOxvGuRRgzG64bvtJ0938xuqzv18d3ZpQhstC" },
        { "abcdefghijklmnopqrstuvwxyz",         "$2a$08$aTsUwsyowQuzRrDqFflhge",    "$2a$08$aTsUwsyowQuzRrDqFflhgekJ8d9/7Z3GV3UcgvzQW3J5zMyrTvlz." },
        { "abcdefghijklmnopqrstuvwxyz",         "$2a$10$fVH8e28OQRj9tqiDXs1e1u",    "$2a$10$fVH8e28OQRj9tqiDXs1e1uxpsjN0c7II7YPKXua2NAKYvM6iQk7dq" },
        { "abcdefghijklmnopqrstuvwxyz",         "$2a$12$D4G5f18o7aMMfwasBL7Gpu",    "$2a$12$D4G5f18o7aMMfwasBL7GpuQWuP3pkrZrOAnqP.bmezbMng.QwJ/pG" },
        { "~!@#$%^&*()      ~!@#$%^&*()PNBFRD", "$2a$06$fPIsBO8qRqkjj273rfaOI.",    "$2a$06$fPIsBO8qRqkjj273rfaOI.HtSV9jLDpTbZn782DC6/t7qT67P6FfO" },
        { "~!@#$%^&*()      ~!@#$%^&*()PNBFRD", "$2a$08$Eq2r4G/76Wv39MzSX262hu",    "$2a$08$Eq2r4G/76Wv39MzSX262huzPz612MZiYHVUJe/OcOql2jo4.9UxTW" },
        { "~!@#$%^&*()      ~!@#$%^&*()PNBFRD", "$2a$10$LgfYWkbzEvQ4JakH7rOvHe",    "$2a$10$LgfYWkbzEvQ4JakH7rOvHe0y8pHKF9OaFgwUZ2q7W2FFZmZzJYlfS" },
        { "~!@#$%^&*()      ~!@#$%^&*()PNBFRD", "$2a$12$WApznUOJfkEGSmYRfnkrPO",    "$2a$12$WApznUOJfkEGSmYRfnkrPOr466oFDCaj4b6HY3EXGvfxm43seyhgC" }
    };

    [TestSvm]
    public static string Test1()
    {
        string result = "";
        for (int ji = 0; ji < _testVectors.Length / 3; ji++)
        {
            var hashInfo = BCrypt.Net.BCrypt.InterrogateHash(_testVectors[ji, 2]);
            result += hashInfo.Version;
        }

        return result;
    }

    [TestSvm]
    public static string Test2()
    {
        string result = "";
        for (int i = 0; i < _testVectors.Length / 3; i++)
        {
            string plain = _testVectors[i, 0];
            string salt = _testVectors[i, 1];
            string expected = _testVectors[i, 2];
            string hashed = BCrypt.Net.BCrypt.HashPassword(plain, salt);
            return hashed;
        }

        return result;
    }

    public static readonly string[,] _differentRevisionTestVectors = {
        { "",                                   "$2a$06$DCq7YPn5Rq63x1Lad4cll.",    "$2b$06$DCq7YPn5Rq63x1Lad4cll.TV4S6ytwfsfvkgY8jIucDrjc8deX1s." },
        { "",                                   "$2b$06$DCq7YPn5Rq63x1Lad4cll.",    "$2b$06$DCq7YPn5Rq63x1Lad4cll.TV4S6ytwfsfvkgY8jIucDrjc8deX1s." },
        { "",                                   "$2x$06$DCq7YPn5Rq63x1Lad4cll.",    "$2b$06$DCq7YPn5Rq63x1Lad4cll.TV4S6ytwfsfvkgY8jIucDrjc8deX1s." },
        { "",                                   "$2y$06$DCq7YPn5Rq63x1Lad4cll.",    "$2b$06$DCq7YPn5Rq63x1Lad4cll.TV4S6ytwfsfvkgY8jIucDrjc8deX1s." },
        { "a",                                  "$2a$06$m0CrhHm10qJ3lXRY.5zDGO",    "$2b$06$m0CrhHm10qJ3lXRY.5zDGO3rS2KdeeWLuGmsfGlMfOxih58VYVfxe" },
        { "a",                                  "$2b$06$m0CrhHm10qJ3lXRY.5zDGO",    "$2b$06$m0CrhHm10qJ3lXRY.5zDGO3rS2KdeeWLuGmsfGlMfOxih58VYVfxe" },
        { "a",                                  "$2x$06$m0CrhHm10qJ3lXRY.5zDGO",    "$2b$06$m0CrhHm10qJ3lXRY.5zDGO3rS2KdeeWLuGmsfGlMfOxih58VYVfxe" },
        { "a",                                  "$2y$06$m0CrhHm10qJ3lXRY.5zDGO",    "$2b$06$m0CrhHm10qJ3lXRY.5zDGO3rS2KdeeWLuGmsfGlMfOxih58VYVfxe" },
        { "abc",                                "$2a$06$If6bvum7DFjUnE9p2uDeDu",    "$2b$06$If6bvum7DFjUnE9p2uDeDu0YHzrHM6tf.iqN8.yx.jNN1ILEf7h0i" },
        { "abc",                                "$2b$06$If6bvum7DFjUnE9p2uDeDu",    "$2b$06$If6bvum7DFjUnE9p2uDeDu0YHzrHM6tf.iqN8.yx.jNN1ILEf7h0i" },
        { "abc",                                "$2x$06$If6bvum7DFjUnE9p2uDeDu",    "$2b$06$If6bvum7DFjUnE9p2uDeDu0YHzrHM6tf.iqN8.yx.jNN1ILEf7h0i" },
        { "abc",                                "$2y$06$If6bvum7DFjUnE9p2uDeDu",    "$2b$06$If6bvum7DFjUnE9p2uDeDu0YHzrHM6tf.iqN8.yx.jNN1ILEf7h0i" },
        { "abcdefghijklmnopqrstuvwxyz",         "$2a$06$.rCVZVOThsIa97pEDOxvGu",    "$2b$06$.rCVZVOThsIa97pEDOxvGuRRgzG64bvtJ0938xuqzv18d3ZpQhstC" },
        { "abcdefghijklmnopqrstuvwxyz",         "$2b$06$.rCVZVOThsIa97pEDOxvGu",    "$2b$06$.rCVZVOThsIa97pEDOxvGuRRgzG64bvtJ0938xuqzv18d3ZpQhstC" },
        { "abcdefghijklmnopqrstuvwxyz",         "$2x$06$.rCVZVOThsIa97pEDOxvGu",    "$2b$06$.rCVZVOThsIa97pEDOxvGuRRgzG64bvtJ0938xuqzv18d3ZpQhstC" },
        { "abcdefghijklmnopqrstuvwxyz",         "$2y$06$.rCVZVOThsIa97pEDOxvGu",    "$2b$06$.rCVZVOThsIa97pEDOxvGuRRgzG64bvtJ0938xuqzv18d3ZpQhstC" },
        { "~!@#$%^&*()      ~!@#$%^&*()PNBFRD", "$2a$06$fPIsBO8qRqkjj273rfaOI.",    "$2b$06$fPIsBO8qRqkjj273rfaOI.HtSV9jLDpTbZn782DC6/t7qT67P6FfO" },
        { "~!@#$%^&*()      ~!@#$%^&*()PNBFRD", "$2b$06$fPIsBO8qRqkjj273rfaOI.",    "$2b$06$fPIsBO8qRqkjj273rfaOI.HtSV9jLDpTbZn782DC6/t7qT67P6FfO" },
        { "~!@#$%^&*()      ~!@#$%^&*()PNBFRD", "$2x$06$fPIsBO8qRqkjj273rfaOI.",    "$2b$06$fPIsBO8qRqkjj273rfaOI.HtSV9jLDpTbZn782DC6/t7qT67P6FfO" },
        { "~!@#$%^&*()      ~!@#$%^&*()PNBFRD", "$2y$06$fPIsBO8qRqkjj273rfaOI.",    "$2b$06$fPIsBO8qRqkjj273rfaOI.HtSV9jLDpTbZn782DC6/t7qT67P6FfO" },
    };

        /**
         * Test method for 'BCrypt.HashPassword(string, string)'
         */
        [TestSvm]
        public static void TestHashPassword()
        {
            for (var i = 0; i < _testVectors.Length / 3; i++)
            {
                var plain = _testVectors[i, 0];
                var salt = _testVectors[i, 1];
                var expected = _testVectors[i, 2];
                var hashed = BCrypt.Net.BCrypt.HashPassword(plain, salt);
            }
        }

        /**
         * Test method for 'BCrypt.GenerateSalt(int)'
         */
        [TestSvm]
        public static void TestGenerateSaltWithWorkFactor()
        {
            BCrypt.Net.BCrypt.GenerateSalt(4);
        }

        /**
         * Test method for 'BCrypt.GenerateSalt()'
         */
        [TestSvm]
        public static bool TestGenerateSalt()
        {
            bool result = true;

            for (var i = 0; i < _testVectors.Length / 3; i++)
            {
                var plain = _testVectors[i, 0];
                var salt = BCrypt.Net.BCrypt.GenerateSalt();
                var hashed1 = BCrypt.Net.BCrypt.HashPassword(plain, salt);
                var hashed2 = BCrypt.Net.BCrypt.HashPassword(plain, hashed1);
                result = result && hashed1 == hashed2;
            }

            return result;
        }

        /**
         * Test method for 'BCrypt.VerifyPassword(string, string)'
         * expecting success
         */
        [TestSvm]
        public static bool TestVerifyPasswordSuccess()
        {
            bool result = true;
            for (var i = 0; i < _testVectors.Length / 3; i++)
            {
                var plain = _testVectors[i, 0];
                var expected = _testVectors[i, 2];
                result = result && BCrypt.Net.BCrypt.Verify(plain, expected);
            }

            return result;
        }


        /**
         * Test method for 'BCrypt.VerifyPassword(string, string)'
         * expecting failure
         */
        [TestSvm]
        public static bool TestVerifyPasswordFailure()
        {
            bool result = true;
            for (var i = 0; i < _testVectors.Length / 3; i++)
            {
                var brokenIndex = (i + 4) % (_testVectors.Length / 3);
                var plain = _testVectors[i, 0];
                var expected = _testVectors[brokenIndex, 2];
                result = result && BCrypt.Net.BCrypt.Verify(plain, expected);
            }
            return result;
        }

        /**
         * Test for correct hashing of non-US-ASCII passwords
         */
        [TestSvm]
        public static bool TestInternationalChars()
        {
            bool result = true;

            var pw1 = "ππππππππ";
            var pw2 = "????????";

            var h1 = BCrypt.Net.BCrypt.HashPassword(pw1, BCrypt.Net.BCrypt.GenerateSalt());
            result = result && BCrypt.Net.BCrypt.Verify(pw2, h1);

            var h2 = BCrypt.Net.BCrypt.HashPassword(pw2, BCrypt.Net.BCrypt.GenerateSalt());
            result = result && BCrypt.Net.BCrypt.Verify(pw1, h2);
            return result;
        }
}
