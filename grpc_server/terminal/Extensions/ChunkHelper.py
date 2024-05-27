class ChunkHelper:
    @staticmethod
    def Chunks(l, n):
        if l is None:
            return
        for i in range(0, len(l), n):
            yield l[i : i + n]
